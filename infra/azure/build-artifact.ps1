param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [ValidateSet("linux-x64", "win-x64")][string]$Runtime = "linux-x64",
    [string]$CommitSha = ""
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Use diretorio novo para gerar artefato imutavel." }
New-Item -ItemType Directory -Path $output | Out-Null
$publish = Join-Path $output "publish"
Push-Location $repoRoot
try {
    & dotnet publish src/Precificador.Web/Precificador.Web.csproj --configuration Release --no-build --output $publish
    if ($LASTEXITCODE -ne 0) { throw "Publish falhou." }
    # ZipArchive cria entradas com '/', independentemente do sistema operacional.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipPath = Join-Path $output "app.zip"
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $publish -Recurse -File) {
            $entry = [System.IO.Path]::GetRelativePath($publish, $file.FullName).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $entry) | Out-Null
        }
    } finally { $zip.Dispose() }
    $bundleName = if ($Runtime -eq "win-x64") { "migration-bundle.exe" } else { "migration-bundle" }
    & dotnet ef migrations bundle --project src/Precificador.Infrastructure/Precificador.Infrastructure.csproj --startup-project src/Precificador.Web/Precificador.Web.csproj --configuration Release --no-build --self-contained --target-runtime $Runtime --output (Join-Path $output $bundleName)
    if ($LASTEXITCODE -ne 0) { throw "Geracao de migration bundle falhou." }
    # A factory EF usa ConnectionStrings__Precificador; nenhum segredo acompanha o bundle.
    '{}' | Set-Content -LiteralPath (Join-Path $output "appsettings.json")
    $hashes = @{}
    foreach ($name in @("app.zip", $bundleName)) {
        $hashes[$name] = (Get-FileHash -LiteralPath (Join-Path $output $name) -Algorithm SHA256).Hash
    }
    @{ commit = $CommitSha; runtime = $Runtime; hashes = $hashes } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output "manifest.json")
} finally { Pop-Location }
