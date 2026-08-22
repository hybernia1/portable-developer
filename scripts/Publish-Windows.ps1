[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\artifacts\publish\PortableDeveloper-offline-win-x64"),
    [string]$LaragonBinPath = "E:\laragon\bin",
    [string]$PhpMyAdminPath = "E:\laragon\etc\apps\phpmyadmin"
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\src\PortableDeveloper.App\PortableDeveloper.App.csproj"
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)

if (Test-Path -LiteralPath $resolvedOutputPath) {
    throw "Cílová složka již existuje. Zvol nový OutputPath, aby se nepřepsala portable data: $resolvedOutputPath"
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $resolvedOutputPath `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    throw "Publikace Portable Developeru selhala (exit code $LASTEXITCODE)."
}

& (Join-Path $PSScriptRoot "Bundle-OfflineDependencies.ps1") `
    -OutputPath $resolvedOutputPath `
    -LaragonBinPath $LaragonBinPath `
    -PhpMyAdminPath $PhpMyAdminPath

if ($LASTEXITCODE -ne 0) {
    throw "Přibalení offline serverových modulů selhalo (exit code $LASTEXITCODE)."
}

Write-Host "Self-contained offline portable výstup: $resolvedOutputPath"
