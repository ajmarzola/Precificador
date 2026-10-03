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
Write-Host "Sintaxe PowerShell, preflight fail-closed e cleanup: OK (sem acesso Azure)."
