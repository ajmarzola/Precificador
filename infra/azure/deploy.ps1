param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$AppName,

    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,

    [string]$SqlDatabaseName = "precificador",
    [string]$AppServicePlanName = "",

    [Parameter(Mandatory = $true)]
    [string]$TenantId,

    [Parameter(Mandatory = $true)]
    [string]$SubscriptionId,
    [string]$OperatorIpAddress = "",
    [string]$ArtifactDirectory = "",
    [string]$RunId = "",
    [string]$RunAttempt = "",
    [string]$CommitSha = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot "common.ps1")
if ([string]::IsNullOrWhiteSpace($AppServicePlanName)) { $AppServicePlanName = "$AppName-plan" }
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$webApp = Assert-DeploymentTarget
$ci = -not [string]::IsNullOrWhiteSpace($ArtifactDirectory)
if ($ci) {
    if ($RunId -notmatch '^\d+$' -or $RunAttempt -notmatch '^\d+$' -or $CommitSha -notmatch '^[a-f0-9]{40}$') {
        throw "Identificadores da execucao/SHA invalidos."
    }
    $artifact = (Resolve-Path -LiteralPath $ArtifactDirectory).Path
    $manifest = Get-Content -LiteralPath (Join-Path $artifact "manifest.json") -Raw | ConvertFrom-Json
    if ($manifest.runtime -ne "linux-x64") { throw "Runtime do artefato invalido." }
    if ($manifest.commit -ne $CommitSha) { throw "Artefato pertence a outro SHA." }
    $zipPath = Join-Path $artifact "app.zip"
    $bundle = Join-Path $artifact "migration-bundle"
    foreach ($file in @("app.zip", "migration-bundle")) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $artifact $file) -Algorithm SHA256).Hash
        if ($hash -ne $manifest.hashes.$file) { throw "Integridade do artefato invalida: $file." }
    }
    if (-not $IsLinux) { throw "Bundle CD requer runner Linux." }
    & chmod +x $bundle
    if ($LASTEXITCODE -ne 0) { throw "Nao foi possivel tornar o bundle executavel." }
    $ruleName = "github-cd-$RunId-$RunAttempt"
} else {
    Push-Location $repoRoot
    try {
        foreach ($arguments in @(
            ,@("tool", "restore"), ,@("restore", "Precificador.slnx"),
            ,@("build", "Precificador.slnx", "--configuration", "Release", "--no-restore"),
            ,@("test", "Precificador.slnx", "--configuration", "Release", "--no-build")
        )) {
            & dotnet @arguments
            if ($LASTEXITCODE -ne 0) { throw "Gate manual falhou." }
        }
        $artifact = Join-Path $repoRoot ("artifacts/azure/manual-" + [guid]::NewGuid().ToString("N"))
        & (Join-Path $PSScriptRoot "build-artifact.ps1") -OutputDirectory $artifact -Runtime $(if ($IsWindows) { "win-x64" } else { "linux-x64" })
        $zipPath = Join-Path $artifact "app.zip"
        $bundle = Join-Path $artifact $(if ($IsWindows) { "migration-bundle.exe" } else { "migration-bundle" })
    } finally { Pop-Location }
    $ruleName = "operator-migration-" + [guid]::NewGuid().ToString("N")
}
$ip = Get-PublicIp -ExplicitIp $OperatorIpAddress
$migrationConnectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Database=$SqlDatabaseName;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;"
Invoke-MigrationWithFirewall -RuleName $ruleName -IpAddress $ip -ConnectionString $migrationConnectionString -Migration {
    Push-Location $artifact
    try {
        & $bundle --connection $migrationConnectionString
        if ($LASTEXITCODE -ne 0) { throw "Migration falhou; ZIP nao sera publicado." }
    } finally { Pop-Location }
}
Invoke-Az @("webapp", "deploy", "-g", $ResourceGroupName, "-n", $AppName,
    "--src-path", $zipPath, "--type", "zip", "--clean", "true", "--only-show-errors") | Out-Null
$url = "https://$($webApp.defaultHostName)"
foreach ($path in @("/", "/Conta/Login")) {
    $healthy = $false
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$url$path" -MaximumRedirection 5 -TimeoutSec 30
            if ([int]$response.StatusCode -eq 200) { $healthy = $true; break }
        } catch { Write-Host "Smoke ${path}: tentativa $attempt/6 sem resposta funcional." }
        if ($attempt -lt 6) { Start-Sleep -Seconds 10 }
    }
    if (-not $healthy) { throw "Smoke falhou: $path." }
}
Write-Host "Deploy e smoke concluidos: $url"
if ($ci -and $env:GITHUB_OUTPUT) { "url=$url" | Add-Content -LiteralPath $env:GITHUB_OUTPUT }
if ($ci -and $env:GITHUB_STEP_SUMMARY) {
    @"
Commit: $CommitSha
Environment: production
Web App: $AppName
Migration: OK
Firewall cleanup: OK
Deploy: OK
Smoke: OK
URL: $url
"@ | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY
}
