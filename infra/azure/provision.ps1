param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,

    [string]$SqlDatabaseName = "precificador",
    [string]$AppServicePlanName = "",
    [string]$SubscriptionId = "",

    [Parameter(Mandatory = $true)]
    [string]$EntraAdminName,

    [Parameter(Mandatory = $true)]
    [string]$EntraAdminObjectId,

    [ValidateSet("User", "Group")]
    [string]$EntraAdminPrincipalType = "User",

    [string]$OperatorIpAddress = "",
    [decimal]$SqlCapacity = 2,
    [int]$AutoPauseDelayMinutes = 60
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($AppServicePlanName)) {
    $AppServicePlanName = "$AppName-plan"
}

function Invoke-Az {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = & az @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        throw "az $($Arguments -join ' ') falhou: $($output -join [Environment]::NewLine)"
    }

    return $output
}

function Invoke-AzJson {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = Invoke-Az ($Arguments + @("-o", "json"))
    $text = $output -join [Environment]::NewLine
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return $text | ConvertFrom-Json
}

function Invoke-AzJsonOrNull {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = & az @Arguments -o json 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        return $null
    }

    $text = $output -join [Environment]::NewLine
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return $text | ConvertFrom-Json
}

function Get-AzTsv {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = Invoke-Az ($Arguments + @("-o", "tsv"))
    return (($output -join [Environment]::NewLine).Trim())
}

function Assert-AzLogin {
    $account = Invoke-AzJson @("account", "show", "--query", "{id:id,name:name,state:state,tenantId:tenantId,user:user.name}")
    if ($null -eq $account -or $account.state -ne "Enabled") {
        throw "A assinatura Azure selecionada precisa estar ativa/Enabled antes do provisionamento."
    }

    if (-not [string]::IsNullOrWhiteSpace($SubscriptionId) -and $account.id -ne $SubscriptionId) {
        Invoke-Az @("account", "set", "--subscription", $SubscriptionId) | Out-Null
        $account = Invoke-AzJson @("account", "show", "--query", "{id:id,name:name,state:state,tenantId:tenantId,user:user.name}")
        if ($account.state -ne "Enabled") {
            throw "A assinatura Azure informada nao esta ativa/Enabled."
        }
    }

    Write-Host "Assinatura validada: $($account.name) ($($account.id)); tenant $($account.tenantId); usuario $($account.user)."
    return $account
}

function Get-DotNetRuntime {
    $output = & az webapp list-runtimes --os linux --runtime dotnet -o json 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "A Azure CLI instalada nao suportou 'az webapp list-runtimes --os linux --runtime dotnet'. Atualize a Azure CLI antes de criar recursos. Erro: $($output -join [Environment]::NewLine)"
    }

    $runtimes = (($output -join [Environment]::NewLine) | ConvertFrom-Json)
    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($runtime in $runtimes) {
        if ($runtime -is [string]) {
            $candidates.Add($runtime)
            continue
        }

        foreach ($property in $runtime.PSObject.Properties) {
            if ($property.Value -is [string]) {
                $candidates.Add($property.Value)
            }
        }
    }

    $selected = $candidates |
        Where-Object { $_ -match "DOTNET" -and $_ -match "10(\.0)?" } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($selected)) {
        throw "Runtime Linux .NET 10 nao encontrado pela Azure CLI. Nenhum fallback de runtime sera criado."
    }

    return $selected.Replace("|", ":")
}

function Assert-FreeCapacity {
    $locations = Invoke-AzJson @("appservice", "list-locations", "--sku", "F1", "--linux-workers-enabled")
    $normalizedLocation = ($Location -replace "\s", "").ToLowerInvariant()
    $locationFound = $false
    foreach ($candidate in $locations) {
        $values = @("name", "displayName", "regionalDisplayName") |
            ForEach-Object {
                $property = $candidate.PSObject.Properties[$_]
                if ($null -ne $property) {
                    $property.Value
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        foreach ($value in $values) {
            if ((($value -replace "\s", "").ToLowerInvariant()) -eq $normalizedLocation) {
                $locationFound = $true
            }
        }
    }

    if (-not $locationFound) {
        throw "App Service F1 Linux nao foi confirmado na regiao '$Location'. Provisionamento interrompido."
    }

    $sqlHelp = (Invoke-Az @("sql", "db", "create", "--help")) -join [Environment]::NewLine
    if ($sqlHelp -notmatch "--use-free-limit" -or
        $sqlHelp -notmatch "--free-limit-exhaustion-behavior" -or
        $sqlHelp -notmatch "AutoPause" -or
        $sqlHelp -notmatch "BillOverUsage") {
        throw "A Azure CLI nao expos os parametros atuais de Azure SQL Free/AutoPause. Provisionamento interrompido."
    }

    $editions = Invoke-AzJson @("sql", "db", "list-editions", "-l", $Location)
    $editionText = $editions | ConvertTo-Json -Depth 100
    if ($editionText -notmatch "GeneralPurpose" -or $editionText -notmatch "Serverless") {
        throw "Azure SQL GeneralPurpose/Serverless nao foi confirmado na regiao '$Location'. Provisionamento interrompido."
    }

    Write-Host "Capacidade gratuita pre-validada: App Service F1 Linux, Azure SQL GeneralPurpose Serverless e AutoPause disponiveis pela CLI."
}

function Get-PublicIp {
    param([string]$ExplicitIp)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitIp)) {
        return $ExplicitIp
    }

    try {
        return (Invoke-RestMethod -Uri "https://api.ipify.org" -UseBasicParsing -TimeoutSec 20).Trim()
    }
    catch {
        throw "Nao foi possivel detectar o IP publico do operador. Informe -OperatorIpAddress para bootstrap/migrations."
    }
}

function Add-SqlFirewallIp {
    param(
        [string]$RuleName,
        [string]$IpAddress
    )

    if ([string]::IsNullOrWhiteSpace($IpAddress) -or $IpAddress -eq "0.0.0.0") {
        return
    }

    Invoke-Az @(
        "sql", "server", "firewall-rule", "create",
        "-g", $ResourceGroupName,
        "-s", $SqlServerName,
        "-n", $RuleName,
        "--start-ip-address", $IpAddress,
        "--end-ip-address", $IpAddress
    ) | Out-Null
}

function Remove-SqlFirewallRuleIfExists {
    param([string]$RuleName)

    $rule = Invoke-AzJsonOrNull @(
        "sql", "server", "firewall-rule", "show",
        "-g", $ResourceGroupName,
        "-s", $SqlServerName,
        "-n", $RuleName
    )

    if ($null -ne $rule) {
        Invoke-Az @(
            "sql", "server", "firewall-rule", "delete",
            "-g", $ResourceGroupName,
            "-s", $SqlServerName,
            "-n", $RuleName
        ) | Out-Null
    }
}

function Invoke-Sql {
    param([Parameter(Mandatory = $true)][string]$Query)

    $token = Invoke-AzJson @(
        "account", "get-access-token",
        "--resource", "https://database.windows.net/"
    )
    if ($null -eq $token -or [string]::IsNullOrWhiteSpace($token.accessToken)) {
        throw "Azure CLI nao retornou access token para o bootstrap do Azure SQL."
    }

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$SqlDatabaseName;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    $connection.AccessToken = $token.accessToken
    $command = $null
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 60
        [void]$command.ExecuteNonQuery()
    }
    finally {
        if ($null -ne $command) {
            $command.Dispose()
        }
        $connection.Dispose()
    }
}

function Assert-SameRegion {
    $webLocation = Get-AzTsv @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName, "--query", "location")
    $sqlLocation = Get-AzTsv @("sql", "server", "show", "-g", $ResourceGroupName, "-n", $SqlServerName, "--query", "location")

    if ((($webLocation -replace "\s", "").ToLowerInvariant()) -ne (($sqlLocation -replace "\s", "").ToLowerInvariant())) {
        throw "Web App e Azure SQL ficaram em regioes diferentes: '$webLocation' e '$sqlLocation'."
    }
}

function Assert-FreeResources {
    $planSku = Get-AzTsv @("appservice", "plan", "show", "-g", $ResourceGroupName, "-n", $AppServicePlanName, "--query", "sku.name")
    if ($planSku -ne "F1") {
        throw "App Service Plan nao esta em F1. Valor encontrado: $planSku."
    }

    $db = Invoke-AzJson @("sql", "db", "show", "-g", $ResourceGroupName, "-s", $SqlServerName, "-n", $SqlDatabaseName)
    $dbText = $db | ConvertTo-Json -Depth 100
    if ($dbText -notmatch '"useFreeLimit"\s*:\s*true') {
        throw "Azure SQL nao confirmou useFreeLimit=true."
    }
    if ($dbText -notmatch '"freeLimitExhaustionBehavior"\s*:\s*"AutoPause"') {
        throw "Azure SQL nao confirmou freeLimitExhaustionBehavior=AutoPause."
    }
}

$account = Assert-AzLogin
$runtime = Get-DotNetRuntime
Assert-FreeCapacity

Write-Host "Runtime Linux .NET 10 descoberto: $runtime"
Write-Host "Criando/atualizando Resource Group..."
Invoke-Az @("group", "create", "-g", $ResourceGroupName, "-l", $Location) | Out-Null

$plan = Invoke-AzJsonOrNull @("appservice", "plan", "show", "-g", $ResourceGroupName, "-n", $AppServicePlanName)
if ($null -eq $plan) {
    Invoke-Az @(
        "appservice", "plan", "create",
        "-g", $ResourceGroupName,
        "-n", $AppServicePlanName,
        "-l", $Location,
        "--sku", "F1",
        "--is-linux"
    ) | Out-Null
}

$webapp = Invoke-AzJsonOrNull @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName)
if ($null -eq $webapp) {
    Invoke-Az @(
        "webapp", "create",
        "-g", $ResourceGroupName,
        "-p", $AppServicePlanName,
        "-n", $AppName,
        "--runtime", $runtime
    ) | Out-Null
}

Invoke-Az @("webapp", "update", "-g", $ResourceGroupName, "-n", $AppName, "--https-only", "true") | Out-Null
Invoke-Az @("webapp", "identity", "assign", "-g", $ResourceGroupName, "-n", $AppName) | Out-Null
$identity = Invoke-AzJson @("webapp", "identity", "show", "-g", $ResourceGroupName, "-n", $AppName)
if ([string]::IsNullOrWhiteSpace($identity.principalId)) {
    throw "System Assigned Managed Identity nao foi confirmada na Web App."
}
$servicePrincipal = Invoke-AzJson @("ad", "sp", "show", "--id", $identity.principalId)
if ($null -eq $servicePrincipal -or [string]::IsNullOrWhiteSpace($servicePrincipal.appId)) {
    throw "Application Client ID da Managed Identity nao foi encontrado no Microsoft Entra."
}

$sqlServer = Invoke-AzJsonOrNull @("sql", "server", "show", "-g", $ResourceGroupName, "-n", $SqlServerName)
if ($null -eq $sqlServer) {
    Invoke-Az @(
        "sql", "server", "create",
        "-g", $ResourceGroupName,
        "-n", $SqlServerName,
        "-l", $Location,
        "--enable-ad-only-auth",
        "--external-admin-principal-type",
        $EntraAdminPrincipalType,
        "--external-admin-name",
        $EntraAdminName,
        "--external-admin-sid",
        $EntraAdminObjectId
    ) | Out-Null
}

$db = Invoke-AzJsonOrNull @("sql", "db", "show", "-g", $ResourceGroupName, "-s", $SqlServerName, "-n", $SqlDatabaseName)
if ($null -eq $db) {
    Invoke-Az @(
        "sql", "db", "create",
        "-g", $ResourceGroupName,
        "-s", $SqlServerName,
        "-n", $SqlDatabaseName,
        "--edition", "GeneralPurpose",
        "--compute-model", "Serverless",
        "--family", "Gen5",
        "--capacity", ([string]$SqlCapacity),
        "--auto-pause-delay", ([string]$AutoPauseDelayMinutes),
        "--backup-storage-redundancy", "Local",
        "--use-free-limit",
        "--free-limit-exhaustion-behavior", "AutoPause"
    ) | Out-Null
}

$connectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Database=$SqlDatabaseName;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
Invoke-Az @(
    "webapp", "config", "appsettings", "set",
    "-g", $ResourceGroupName,
    "-n", $AppName,
    "--settings",
    "ASPNETCORE_ENVIRONMENT=Production",
    "ConnectionStrings__Precificador=$connectionString"
) | Out-Null

$outbound = Get-AzTsv @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName, "--query", "outboundIpAddresses")
$possibleOutbound = Get-AzTsv @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName, "--query", "possibleOutboundIpAddresses")
$webIps = @($outbound, $possibleOutbound) -join ","
$webIps = $webIps.Split(",", [System.StringSplitOptions]::RemoveEmptyEntries) |
    ForEach-Object { $_.Trim() } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Sort-Object -Unique

if ($webIps.Count -eq 0) {
    throw "Nao foi possivel descobrir outboundIpAddresses/possibleOutboundIpAddresses da Web App."
}

$index = 1
foreach ($ip in $webIps) {
    Add-SqlFirewallIp -RuleName ("webapp-out-{0:000}" -f $index) -IpAddress $ip
    $index++
}

$operatorRule = "operator-bootstrap-temp"
$operatorIp = Get-PublicIp -ExplicitIp $OperatorIpAddress
try {
    Add-SqlFirewallIp -RuleName $operatorRule -IpAddress $operatorIp

    $principalName = $AppName.Replace("'", "''")
    $clientId = $servicePrincipal.appId
    $bootstrapSql = @"
DECLARE @principalName sysname = N'$principalName';
DECLARE @clientId uniqueidentifier = '$clientId';
DECLARE @expectedSid varbinary(16) = CONVERT(varbinary(16), @clientId);
DECLARE @sql nvarchar(max);

IF EXISTS (
    SELECT 1
    FROM sys.database_principals
    WHERE name = @principalName AND sid <> @expectedSid)
BEGIN
    IF IS_ROLEMEMBER(N'db_datareader', @principalName) = 1
    BEGIN
        SET @sql = N'ALTER ROLE [db_datareader] DROP MEMBER ' + QUOTENAME(@principalName) + N';';
        EXEC (@sql);
    END;
    IF IS_ROLEMEMBER(N'db_datawriter', @principalName) = 1
    BEGIN
        SET @sql = N'ALTER ROLE [db_datawriter] DROP MEMBER ' + QUOTENAME(@principalName) + N';';
        EXEC (@sql);
    END;
    SET @sql = N'DROP USER ' + QUOTENAME(@principalName) + N';';
    EXEC (@sql);
END;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @principalName)
BEGIN
    DECLARE @sid nvarchar(max) = CONVERT(varchar(max), @expectedSid, 1);
    SET @sql = N'CREATE USER ' + QUOTENAME(@principalName) + N' WITH SID = ' + @sid + N', TYPE = E;';
    EXEC (@sql);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datareader' AND m.name = @principalName)
BEGIN
    SET @sql = N'ALTER ROLE [db_datareader] ADD MEMBER ' + QUOTENAME(@principalName) + N';';
    EXEC (@sql);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datawriter' AND m.name = @principalName)
BEGIN
    SET @sql = N'ALTER ROLE [db_datawriter] ADD MEMBER ' + QUOTENAME(@principalName) + N';';
    EXEC (@sql);
END;

IF EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE m.name = @principalName AND r.name IN (N'db_owner', N'db_ddladmin'))
BEGIN
    THROW 51000, 'Managed Identity recebeu role DDL proibida.', 1;
END;
"@
    Invoke-Sql -Query $bootstrapSql
}
finally {
    Remove-SqlFirewallRuleIfExists -RuleName $operatorRule
}

Assert-SameRegion
Assert-FreeResources

$hostName = Get-AzTsv @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName, "--query", "defaultHostName")
Write-Host "Provisionamento concluido sem fallback pago."
Write-Host "URL: https://$hostName"
Write-Host "App Service: F1/Free. Azure SQL: Free Limit + AutoPause. Managed Identity: db_datareader + db_datawriter."
