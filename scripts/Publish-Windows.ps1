[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\artifacts\publish\PortableDeveloper-offline-win-x64"),
    [string]$LaragonBinPath = "E:\laragon\bin",
    [string]$PhpMyAdminPath = "E:\laragon\etc\apps\phpmyadmin",
    [string]$GeckoDriverArchivePath = (Join-Path $PSScriptRoot "..\downloads\bundle-cache\geckodriver-v0.37.1-win64.zip"),
    [string]$ComposerPath = (Join-Path $PSScriptRoot "..\downloads\bundle-cache\composer-2.10.2.phar"),

    [ValidateRange(1, 20)]
    [int]$ReleasesToKeep = 2
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
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=false `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "Publikace Portable Developeru selhala (exit code $LASTEXITCODE)."
}

& (Join-Path $PSScriptRoot "Bundle-OfflineDependencies.ps1") `
    -OutputPath $resolvedOutputPath `
    -LaragonBinPath $LaragonBinPath `
    -PhpMyAdminPath $PhpMyAdminPath `
    -GeckoDriverArchivePath $GeckoDriverArchivePath `
    -ComposerPath $ComposerPath

if ($LASTEXITCODE -ne 0) {
    throw "Přibalení offline serverových modulů selhalo (exit code $LASTEXITCODE)."
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$releaseDocuments = @(
    "LICENSE",
    "PRIVACY.md",
    "THIRD-PARTY-NOTICES.md"
)

foreach ($document in $releaseDocuments) {
    $sourcePath = Join-Path $repositoryRoot $document
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Povinný release dokument chybí: $sourcePath"
    }

    Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $resolvedOutputPath $document) -Force
}

Write-Host "Self-contained offline portable výstup: $resolvedOutputPath"

& (Join-Path $PSScriptRoot "Cleanup-Releases.ps1") `
    -PublishRoot (Split-Path -Parent $resolvedOutputPath) `
    -Keep $ReleasesToKeep
