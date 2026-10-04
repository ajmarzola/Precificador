param(
    [Parameter(Mandatory = $true)][string]$ResourceGroupName,
    [Parameter(Mandatory = $true)][string]$AppName,
    [Parameter(Mandatory = $true)][string]$SqlServerName,
    [string]$SqlDatabaseName = "precificador",
    [Parameter(Mandatory = $true)][string]$AppServicePlanName,
    [Parameter(Mandatory = $true)][string]$TenantId,
    [Parameter(Mandatory = $true)][string]$SubscriptionId,
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')][string]$Repository = "ajmarzola/Precificador",
    [string]$IdentityName = "mi-precificador-cd",
    [string]$OperatorIpAddress = ""
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot "common.ps1")
# Modulo administrativo; nao e dependencia dos runners de CD.
Import-Module SqlServer -ErrorAction Stop
$webApp = Assert-DeploymentTarget
$server = Invoke-AzJson @("sql", "server", "show", "-g", $ResourceGroupName, "-n", $SqlServerName)
$database = Invoke-AzJson @("sql", "db", "show", "-g", $ResourceGroupName, "-s", $SqlServerName, "-n", $SqlDatabaseName)
$expectedDatabaseId = "$($server.id)/databases/$SqlDatabaseName"
if ($null -eq $database -or $database.id -ne $expectedDatabaseId) {
    throw "Database nao corresponde ao recurso esperado; bootstrap interrompido."
}
$plan = Invoke-AzJson @("appservice", "plan", "show", "-g", $ResourceGroupName, "-n", $AppServicePlanName)
$identity = Get-CdIdentityOrNull -IdentityName $IdentityName
if ($null -eq $identity) {
    $identity = Invoke-AzJson @("identity", "create", "-g", $ResourceGroupName, "-n", $IdentityName, "-l", $webApp.location)
}
if ($identity.tenantId -ne $TenantId -or $identity.principalId -eq $webApp.identity.principalId) {
    throw "Identidade CD invalida ou igual a runtime."
}
$subject = "repo:${Repository}:environment:production"
$credentials = @(Get-CdFederatedCredentials -IdentityName $IdentityName -Subject $subject)
if ($credentials.Count -eq 0) {
    Invoke-Az @("identity", "federated-credential", "create", "-g", $ResourceGroupName, "--identity-name", $IdentityName,
        "-n", "github-production", "--issuer", "https://token.actions.githubusercontent.com", "--subject", $subject,
        "--audiences", "api://AzureADTokenExchange") | Out-Null
}
$desired = @(
    @{ Scope = $webApp.id; Role = "Website Contributor" },
    @{ Scope = $server.id; Role = "SQL Security Manager" },
    # Website Contributor na Web App nao concede leitura no recurso irmao serverFarm.
    @{ Scope = $plan.id; Role = "Reader" },
    # SQL Security Manager no servidor nao concede databases/read ao preflight.
    @{ Scope = $database.id; Role = "Reader" }
)
$assignments = @(Invoke-AzJson @("role", "assignment", "list", "--assignee-object-id", $identity.principalId, "--all", "--include-inherited"))
foreach ($assignment in $assignments) {
    if (-not ($desired | Where-Object { $_.Scope -eq $assignment.scope -and $_.Role -eq $assignment.roleDefinitionName })) {
        throw "Identidade CD possui RBAC fora dos escopos/roles permitidos. Revisao administrativa necessaria."
    }
}
foreach ($target in $desired) {
    $definitions = @(Invoke-AzJson @("role", "definition", "list", "--name", $target.Role))
    if ($definitions.Count -ne 1 -or $definitions[0].roleName -ne $target.Role -or $definitions[0].roleType -ne "BuiltInRole") {
        throw "Role built-in nao confirmada: $($target.Role). Nenhum fallback amplo."
    }
    if (-not ($assignments | Where-Object { $_.scope -eq $target.Scope -and $_.roleDefinitionName -eq $target.Role })) {
        Invoke-Az @("role", "assignment", "create", "--assignee-object-id", $identity.principalId,
            "--assignee-principal-type", "ServicePrincipal", "--role", $definitions[0].name, "--scope", $target.Scope) | Out-Null
    }
    $confirmed = @(Invoke-AzJson @("role", "assignment", "list", "--assignee-object-id", $identity.principalId, "--scope", $target.Scope))
    if (-not ($confirmed | Where-Object { $_.scope -eq $target.Scope -and $_.roleDefinitionName -eq $target.Role })) {
        throw "RBAC nao confirmado: $($target.Role)."
    }
}
$principalName = $IdentityName.Replace("'", "''")
$runtimeName = $AppName.Replace("'", "''")
$clientId = ([guid]$identity.clientId).ToString()
$query = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @name sysname = N'$principalName';
DECLARE @sid varbinary(16) = CONVERT(varbinary(16), CONVERT(uniqueidentifier, '$clientId'));
DECLARE @sql nvarchar(max);
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @name AND (sid <> @sid OR type <> 'E'))
    THROW 51000, 'Usuario CD existente possui SID/tipo divergente.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @name)
BEGIN
    SET @sql = N'CREATE USER ' + QUOTENAME(@name) + N' WITH SID = ' + CONVERT(varchar(max), @sid, 1) + N', TYPE = E;';
    EXEC (@sql);
END;
IF EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE m.name = @name AND r.name NOT IN ('db_ddladmin', 'db_datareader', 'db_datawriter'))
    THROW 51000, 'Usuario CD possui role SQL fora do contrato.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$runtimeName')
    THROW 51000, 'Usuario runtime nao encontrado.', 1;
IF EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE m.name = N'$runtimeName' AND r.name NOT IN ('db_datareader', 'db_datawriter'))
    THROW 51000, 'Runtime possui role SQL proibida.', 1;
DECLARE @role sysname;
DECLARE roles CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.database_principals
    WHERE name IN ('db_ddladmin', 'db_datareader', 'db_datawriter');
OPEN roles;
FETCH NEXT FROM roles INTO @role;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF IS_ROLEMEMBER(@role, @name) <> 1
    BEGIN
        SET @sql = N'ALTER ROLE ' + QUOTENAME(@role) + N' ADD MEMBER ' + QUOTENAME(@name) + N';';
        EXEC (@sql);
    END;
    IF IS_ROLEMEMBER(@role, @name) <> 1 THROW 51000, 'Role de migration nao confirmada.', 1;
    FETCH NEXT FROM roles INTO @role;
END;
CLOSE roles;
DEALLOCATE roles;
COMMIT;
"@
$ruleName = "operator-cd-bootstrap-" + [guid]::NewGuid().ToString("N")
$token = $null
try {
    Add-SqlFirewallIp -RuleName $ruleName -IpAddress (Get-PublicIp -ExplicitIp $OperatorIpAddress)
    $token = Invoke-AzJson @("account", "get-access-token", "--resource", "https://database.windows.net/")
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Invoke-Sqlcmd -ServerInstance "$SqlServerName.database.windows.net" -Database $SqlDatabaseName -AccessToken $token.accessToken -Query $query -ConnectionTimeout 60 -QueryTimeout 60 -AbortOnError -ErrorAction Stop | Out-Null
            break
        } catch {
            # Somente auto-resume; autorizacao/DDL invalida nao recebem retry.
            if ($_.Exception.ToString() -notmatch '40613' -or $attempt -eq 5) {
                throw "Bootstrap SQL CD falhou; verifique acesso Entra admin e permissoes SQL."
            }
            Start-Sleep -Seconds ([Math]::Pow(2, $attempt))
        }
    }
} finally {
    $token = $null
    Remove-SqlFirewallRuleIfExists -RuleName $ruleName
}
$confirmedCredentials = @(Get-CdFederatedCredentials -IdentityName $IdentityName -Subject $subject)
if ($confirmedCredentials.Count -eq 0) {
    throw "Federacao nao confirmada."
}
Write-Host "Configure estas variables nao secretas no GitHub Environment production:"
@{
    AZURE_CLIENT_ID = $identity.clientId; AZURE_TENANT_ID = $TenantId; AZURE_SUBSCRIPTION_ID = $SubscriptionId
    AZURE_RESOURCE_GROUP = $ResourceGroupName; AZURE_WEBAPP_NAME = $AppName
    AZURE_SQL_SERVER = $SqlServerName; AZURE_SQL_DATABASE = $SqlDatabaseName; AZURE_APP_SERVICE_PLAN = $AppServicePlanName
}.GetEnumerator() | Sort-Object Name | ForEach-Object { Write-Host "$($_.Name)=$($_.Value)" }
