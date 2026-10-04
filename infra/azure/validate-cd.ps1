# Validacao local dos guards e cleanup, sem acessar/mutar Azure.
# Execute em PowerShell 7. Nao substitui o primeiro CD real pos-merge.
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
Get-ChildItem -LiteralPath $PSScriptRoot -Filter *.ps1 | ForEach-Object {
    $tokens = $null
    $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors)
    if ($errors.Count) { throw "Sintaxe PowerShell invalida em $($_.Name): $errors" }
}
. (Join-Path $PSScriptRoot "common.ps1")
$invokeAzImplementation = (Get-Item Function:Invoke-Az).ScriptBlock
$TenantId = "tenant-test"
$SubscriptionId = "subscription-test"
$ResourceGroupName = "rg-test"
$AppServicePlanName = "plan-test"
$AppName = "app-test"
$SqlServerName = "sql-test"
$SqlDatabaseName = "db-test"
$script:scenario = "healthy"
$script:rulePresent = $true
$script:writes = 0
function Get-AzTsv {
    param([string[]]$Arguments)
    if ($Arguments[-1] -eq "sku.name") {
        if ($script:scenario -eq "paid-plan") { return "B1" }
        return "F1"
    }
    return "/plans/expected"
}
function Invoke-AzJson {
    param([string[]]$Arguments)
    switch ($Arguments[0]) {
        "account" {
            return @{ state = "Enabled"; name = "Test"; tenantId = $(if ($script:scenario -eq "tenant") { "other" } else { $TenantId });
                id = $(if ($script:scenario -eq "subscription") { "other" } else { $SubscriptionId }) }
        }
        "sql" {
            if ($Arguments[1] -eq "db") {
                return @{ useFreeLimit = ($script:scenario -ne "no-free"); freeLimitExhaustionBehavior = $(if ($script:scenario -eq "bill") { "BillOverUsage" } else { "AutoPause" }) }
            }
            if ($script:scenario -eq "cleanup-read-failure") { throw "Falha de leitura simulada" }
            if ($script:rulePresent) { return @{ name = "test-rule" } }
            return @()
        }
        "webapp" {
            if ($Arguments[1] -eq "show") {
                return @{ httpsOnly = ($script:scenario -ne "http");
                    serverFarmId = $(if ($script:scenario -eq "other-plan") { "/plans/other" } else { "/plans/expected" }); identity = @{ principalId = "runtime" } }
            }
            $connection = "Server=tcp:sql-test.database.windows.net,1433;Database=db-test;Authentication=Active Directory Managed Identity;Connection Timeout=60;"
            switch ($script:scenario) {
                "password" { $connection += "Password=example;" }
                "uid" { $connection += "UID=example;" }
                "other-db" { $connection = $connection.Replace("db-test", "other") }
                "other-server" { $connection = $connection.Replace("sql-test", "other") }
                "timeout" { $connection = $connection.Replace("Timeout=60", "Timeout=15") }
                "authentication" { $connection = $connection.Replace("Active Directory Managed Identity", "Active Directory Default") }
            }
            return @{ name = "ConnectionStrings__Precificador"; value = $connection }
        }
    }
    throw "Comando inesperado no teste"
}
function Invoke-Az {
    param([string[]]$Arguments)
    $script:writes++
    if ($script:scenario -eq "cleanup-delete-failure") { throw "Falha de delete simulada" }
    if ($script:scenario -ne "cleanup-remains") { $script:rulePresent = $false }
}
function Assert-Rejected {
    param([scriptblock]$Action)
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw "Cenario deveria falhar: $script:scenario" }
}
Assert-DeploymentTarget | Out-Null
foreach ($case in @("tenant", "subscription", "paid-plan", "no-free", "bill", "http", "other-plan", "password", "uid", "other-db", "other-server", "timeout", "authentication")) {
    $script:scenario = $case
    Assert-Rejected { Assert-DeploymentTarget }
    if ($script:writes -ne 0) { throw "Preflight executou operacao mutavel." }
}
$script:scenario = "healthy"
Remove-SqlFirewallRuleIfExists -RuleName "test-rule"
if ($script:rulePresent -or $script:writes -ne 1) { throw "Cleanup nao removeu regra." }
Remove-SqlFirewallRuleIfExists -RuleName "test-rule"
if ($script:writes -ne 1) { throw "Cleanup nao e idempotente." }
foreach ($case in @("cleanup-read-failure", "cleanup-delete-failure", "cleanup-remains")) {
    $script:scenario = $case
    $script:rulePresent = $true
    Assert-Rejected { Remove-SqlFirewallRuleIfExists -RuleName "test-rule" }
}
foreach ($ip in @("0.0.0.0", "::1", "not-an-ip")) {
    Assert-Rejected { Add-SqlFirewallIp -RuleName "test-rule" -IpAddress $ip }
}
function Add-SqlFirewallIp {
    param([string]$RuleName, [string]$IpAddress)
    $script:events.Add("firewall")
    $script:rulePresent = $true
}
$previousConnection = $env:ConnectionStrings__Precificador
try {
    $env:ConnectionStrings__Precificador = "previous-test"
    foreach ($case in @("healthy", "migration-failure", "cleanup-delete-failure")) {
        $script:scenario = $case
        $script:events = [System.Collections.Generic.List[string]]::new()
        $published = $false
        $failed = $false
        try {
            Invoke-MigrationWithFirewall -RuleName "test-rule" -IpAddress "192.0.2.1" -ConnectionString "migration-test" -Migration {
                if ($env:ConnectionStrings__Precificador -ne "migration-test") { throw "Conexao de migration ausente." }
                $script:events.Add("migration")
                if ($script:scenario -eq "migration-failure") { throw "Migration simulada falhou" }
            }
            if ($script:rulePresent) { throw "Publicacao antes de cleanup." }
            $published = $true
        } catch { $failed = $true }
        if ($script:scenario -eq "healthy" -and (-not $published -or $failed)) { throw "Fluxo saudavel falhou." }
        if ($script:scenario -ne "healthy" -and ($published -or -not $failed)) { throw "Falha permitiu publicacao." }
        if ($env:ConnectionStrings__Precificador -ne "previous-test") { throw "Ambiente nao foi restaurado." }
        if (($script:events -join ',') -ne "firewall,migration") { throw "Ordem incorreta." }
        if ($script:scenario -ne "cleanup-delete-failure" -and $script:rulePresent) { throw "Regra temporaria permaneceu." }
    }
} finally { $env:ConnectionStrings__Precificador = $previousConnection }
$TenantId = "11111111-1111-1111-1111-111111111111"
$script:identityReadScenario = "existing"
function az {
    $Arguments = @($args)
    if (($Arguments[0..1] -join ' ') -ne 'identity show') { throw "Lookup executou comando inesperado." }
    $global:LASTEXITCODE = 0
    switch ($script:identityReadScenario) {
        "missing" { $global:LASTEXITCODE = 3; return "ERROR: (ResourceNotFound) Identity not found." }
        "forbidden" { $global:LASTEXITCODE = 1; return "ERROR: (AuthorizationFailed) Access denied." }
        "network" { $global:LASTEXITCODE = 1; return "Connection failed." }
        "missing-group" { $global:LASTEXITCODE = 3; return "ERROR: (ResourceGroupNotFound) Group not found." }
        "empty" { return "" }
        "null" { return "null" }
        "invalid-json" { return "not-json" }
        "invalid-shape" { return '{"unexpected":true}' }
        "array" { return '[{"name":"mi-precificador-cd"}]' }
    }
    $payload = @{
        name = "mi-precificador-cd"
        id = $(if ($script:identityReadScenario -eq "wrong-id") { "/other" } else {
            "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.ManagedIdentity/userAssignedIdentities/mi-precificador-cd"
        })
        tenantId = $TenantId
        clientId = "22222222-2222-2222-2222-222222222222"
        principalId = "33333333-3333-3333-3333-333333333333"
    }
    if ($script:identityReadScenario -eq "valid-object-array") { return (ConvertTo-Json -InputObject @($payload)) }
    $payload | ConvertTo-Json
}
$identity = Get-CdIdentityOrNull -IdentityName "mi-precificador-cd"
if ($null -eq $identity -or $identity.name -ne "mi-precificador-cd") { throw "Identidade existente nao foi localizada." }
$script:identityReadScenario = "missing"
if ($null -ne (Get-CdIdentityOrNull -IdentityName "mi-precificador-cd")) { throw "Ausencia confirmada nao retornou null." }
foreach ($case in @("forbidden", "network", "missing-group", "empty", "null", "invalid-json", "invalid-shape", "array", "valid-object-array", "wrong-id")) {
    $script:identityReadScenario = $case
    Assert-Rejected { Get-CdIdentityOrNull -IdentityName "mi-precificador-cd" }
}
Set-Item Function:Invoke-Az -Value $invokeAzImplementation
$subject = "repo:ajmarzola/Precificador:environment:production"
function az {
    $Arguments = @($args)
    if (($Arguments[0..2] -join ' ') -ne 'identity federated-credential list' -or
        $Arguments[3] -ne '-g' -or $Arguments[4] -ne $ResourceGroupName -or
        $Arguments[5] -ne '--identity-name' -or $Arguments[6] -ne 'mi-precificador-cd') {
        throw "Leitura de federacao executou comando inesperado."
    }
    $global:LASTEXITCODE = 0
    switch ($script:scenario) {
        "credentials-empty" { return '[]' }
        "credentials-forbidden" { $global:LASTEXITCODE = 1; return 'ERROR: (AuthorizationFailed) Access denied.' }
        "credentials-network" { $global:LASTEXITCODE = 1; return 'Connection failed.' }
        "credentials-missing" { $global:LASTEXITCODE = 3; return 'ERROR: (ResourceNotFound) Identity not found.' }
        "credentials-envelope" { return '{"value":[],"nextLink":"https://example.invalid/page"}' }
        "credentials-object" { return '{"name":"github-production"}' }
        "credentials-null" { return 'null' }
        "credentials-blank" { return '' }
        "credentials-json" { return 'not-json' }
        "credentials-null-item" { return '[null]' }
        "credentials-scalar-item" { return '[42]' }
        "credentials-nested" { return '[[]]' }
        "credentials-shape" { return '[{"unexpected":true}]' }
    }
    $credential = @{
        name = 'github-production'; issuer = 'https://token.actions.githubusercontent.com'
        subject = $subject; audiences = @('api://AzureADTokenExchange')
    }
    switch ($script:scenario) {
        "credentials-issuer" { $credential.issuer = 'https://example.invalid' }
        "credentials-subject" { $credential.subject = 'repo:other/repo:environment:production' }
        "credentials-audience" { $credential.audiences = @('other') }
        "credentials-audiences-empty" { $credential.audiences = @() }
        "credentials-audiences-extra" { $credential.audiences = @('api://AzureADTokenExchange', 'other') }
        "credentials-audiences-scalar" { $credential.audiences = 'api://AzureADTokenExchange' }
        "credentials-field-type" { $credential.issuer = @('https://token.actions.githubusercontent.com') }
    }
    if ($script:scenario -eq 'credentials-mixed') {
        return (ConvertTo-Json -InputObject @($credential, @{ unexpected = $true }) -Depth 5)
    }
    return (ConvertTo-Json -InputObject @($credential) -Depth 5)
}
$script:scenario = 'credentials-empty'
$credentials = @(Get-CdFederatedCredentials -IdentityName 'mi-precificador-cd' -Subject $subject)
if ($credentials.Count -ne 0) { throw "Colecao vazia nao normalizada." }
$script:scenario = 'credentials-valid'
$credentials = @(Get-CdFederatedCredentials -IdentityName 'mi-precificador-cd' -Subject $subject)
if ($credentials.Count -ne 1 -or $credentials[0].name -ne 'github-production') {
    throw "Credential valida nao foi preservada como item unico."
}
foreach ($case in @('credentials-forbidden', 'credentials-network', 'credentials-missing',
    'credentials-envelope', 'credentials-object', 'credentials-null', 'credentials-blank', 'credentials-json',
    'credentials-null-item', 'credentials-scalar-item', 'credentials-nested', 'credentials-shape',
    'credentials-issuer', 'credentials-subject', 'credentials-audience', 'credentials-audiences-empty',
    'credentials-audiences-extra', 'credentials-audiences-scalar', 'credentials-field-type', 'credentials-mixed')) {
    $script:scenario = $case
    Assert-Rejected { Get-CdFederatedCredentials -IdentityName 'mi-precificador-cd' -Subject $subject }
}
Write-Host "Sintaxe PowerShell, preflight, cleanup, lookup UAMI e federacao fail-closed: OK (sem acesso Azure)."
