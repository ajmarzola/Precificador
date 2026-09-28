param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,

    [string]$SqlDatabaseName = "precificador",
    [string]$AppServicePlanName = "",
    [string]$SubscriptionId = "",
    [string]$OperatorIpAddress = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($AppServicePlanName)) {
    $AppServicePlanName = "$AppName-plan"
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$publishDir = Join-Path $repoRoot "artifacts\azure\publish"
$zipPath = Join-Path $repoRoot "artifacts\azure\precificador.zip"

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
        throw "A assinatura Azure selecionada precisa estar ativa/Enabled antes do deploy."
    }

    if (-not [string]::IsNullOrWhiteSpace($SubscriptionId) -and $account.id -ne $SubscriptionId) {
        Invoke-Az @("account", "set", "--subscription", $SubscriptionId) | Out-Null
        $account = Invoke-AzJson @("account", "show", "--query", "{id:id,name:name,state:state,tenantId:tenantId,user:user.name}")
        if ($account.state -ne "Enabled") {
            throw "A assinatura Azure informada nao esta ativa/Enabled."
        }
    }

    Write-Host "Assinatura validada: $($account.name) ($($account.id)); tenant $($account.tenantId); usuario $($account.user)."
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
        throw "Nao foi possivel detectar o IP publico do operador. Informe -OperatorIpAddress para migrations."
    }
}

function Add-SqlFirewallIp {
    param(
        [string]$RuleName,
        [string]$IpAddress
    )

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

function Assert-FreeResources {
    $planSku = Get-AzTsv @("appservice", "plan", "show", "-g", $ResourceGroupName, "-n", $AppServicePlanName, "--query", "sku.name")
    if ($planSku -ne "F1") {
        throw "Deploy interrompido: App Service Plan nao esta em F1. Valor encontrado: $planSku."
    }

    $db = Invoke-AzJson @("sql", "db", "show", "-g", $ResourceGroupName, "-s", $SqlServerName, "-n", $SqlDatabaseName)
    $dbText = $db | ConvertTo-Json -Depth 100
    if ($dbText -notmatch '"useFreeLimit"\s*:\s*true') {
        throw "Deploy interrompido: Azure SQL nao confirmou useFreeLimit=true."
    }
    if ($dbText -notmatch '"freeLimitExhaustionBehavior"\s*:\s*"AutoPause"') {
        throw "Deploy interrompido: Azure SQL nao confirmou freeLimitExhaustionBehavior=AutoPause."
    }
}

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') falhou."
    }
}

function Assert-NoPendingMigrations {
    $output = & dotnet ef migrations list `
        --project "src\Precificador.Infrastructure\Precificador.Infrastructure.csproj" `
        --startup-project "src\Precificador.Web\Precificador.Web.csproj" `
        --configuration Release `
        --no-build 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "Nao foi possivel listar migrations: $($output -join [Environment]::NewLine)"
    }

    $text = $output -join [Environment]::NewLine
    if ($text -match "\(Pending\)") {
        throw "Ainda existem migrations pendentes apos database update."
    }
}

Assert-AzLogin
Assert-FreeResources

$webApp = Invoke-AzJson @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName)
if ($webApp.httpsOnly -ne $true) {
    throw "Deploy interrompido: HTTPS Only nao esta habilitado na Web App."
}

$settings = Invoke-AzJson @("webapp", "config", "appsettings", "list", "-g", $ResourceGroupName, "-n", $AppName)
$connectionSetting = $settings | Where-Object { $_.name -eq "ConnectionStrings__Precificador" } | Select-Object -First 1
if ($null -eq $connectionSetting -or $connectionSetting.value -notmatch "Active Directory Managed Identity") {
    throw "Deploy interrompido: ConnectionStrings__Precificador nao confirma Managed Identity passwordless."
}
if ($connectionSetting.value -match "Password\s*=" -or $connectionSetting.value -match "User ID\s*=") {
    throw "Deploy interrompido: connection string contem credencial."
}

Invoke-DotNet @("tool", "restore")
Invoke-DotNet @("restore", "Precificador.slnx")
Invoke-DotNet @("build", "Precificador.slnx", "--configuration", "Release", "--no-restore")
Invoke-DotNet @("test", "Precificador.slnx", "--configuration", "Release", "--no-build")

$operatorRule = "operator-migration-temp"
$operatorIp = Get-PublicIp -ExplicitIp $OperatorIpAddress
$migrationConnectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Database=$SqlDatabaseName;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

try {
    Add-SqlFirewallIp -RuleName $operatorRule -IpAddress $operatorIp
    $env:ConnectionStrings__Precificador = $migrationConnectionString

    Invoke-DotNet @(
        "ef", "database", "update",
        "--project", "src\Precificador.Infrastructure\Precificador.Infrastructure.csproj",
        "--startup-project", "src\Precificador.Web\Precificador.Web.csproj",
        "--configuration", "Release",
        "--no-build"
    )

    Assert-NoPendingMigrations
}
finally {
    Remove-Item Env:\ConnectionStrings__Precificador -ErrorAction SilentlyContinue
    Remove-SqlFirewallRuleIfExists -RuleName $operatorRule
}

$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)
$resolvedZipPath = [System.IO.Path]::GetFullPath($zipPath)
$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts\azure"))
if (-not $resolvedPublishDir.StartsWith($resolvedArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    -not $resolvedZipPath.StartsWith($resolvedArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Caminho de artefato inesperado. Deploy interrompido."
}

if (Test-Path $resolvedPublishDir) {
    Remove-Item -LiteralPath $resolvedPublishDir -Recurse -Force
}
if (Test-Path $resolvedZipPath) {
    Remove-Item -LiteralPath $resolvedZipPath -Force
}
New-Item -ItemType Directory -Path $resolvedPublishDir -Force | Out-Null

Invoke-DotNet @(
    "publish", "src\Precificador.Web\Precificador.Web.csproj",
    "--configuration", "Release",
    "--no-build",
    "--output", $resolvedPublishDir
)

$tar = Get-Command tar.exe -ErrorAction Stop
& $tar.Source -a -c -f $resolvedZipPath -C $resolvedPublishDir .
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao criar pacote ZIP portavel para o App Service Linux."
}

Invoke-Az @(
    "webapp", "deploy",
    "-g", $ResourceGroupName,
    "-n", $AppName,
    "--src-path", $resolvedZipPath,
    "--type", "zip",
    "--clean", "true"
) | Out-Null

$hostName = Get-AzTsv @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName, "--query", "defaultHostName")
$url = "https://$hostName"
$response = Invoke-WebRequest -Uri $url -UseBasicParsing -MaximumRedirection 5 -TimeoutSec 60
if ([int]$response.StatusCode -ge 500) {
    throw "Smoke HTTPS falhou com status $($response.StatusCode)."
}

Write-Host "Deploy concluido: $url"
Write-Host "Migration explicita aplicada, IP temporario removido e smoke HTTPS executado."
