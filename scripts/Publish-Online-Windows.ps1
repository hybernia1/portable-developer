[CmdletBinding()]
param(
    [string]$Version = "0.8.0",
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\artifacts\publish\PortableDeveloper-win-x64-$Version"),
    [string]$DependencyCatalogPath = (Join-Path $PSScriptRoot "..\catalog\dependencies.lock.json"),
    [string]$DependencyCachePath = (Join-Path $PSScriptRoot "..\downloads\dependencies"),
    [switch]$OfflineDependencies,

    [ValidateRange(1, 20)]
    [int]$ReleasesToKeep = 2
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$projectPath = Join-Path $repositoryRoot "src\PortableDeveloper.App\PortableDeveloper.App.csproj"
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$publishRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "artifacts\publish"))
if (-not $resolvedOutput.StartsWith($publishRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Online release output must remain below: $publishRoot"
}

if (Test-Path -LiteralPath $resolvedOutput) {
    throw "Release output already exists and will not be overwritten: $resolvedOutput"
}

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$projectVersion = [string]$project.Project.PropertyGroup.Version
if ($projectVersion -ne $Version) {
    throw "Requested version $Version does not match the application version $projectVersion."
}

& (Join-Path $PSScriptRoot "Fetch-Dependencies.ps1") `
    -CatalogPath $DependencyCatalogPath `
    -CachePath $DependencyCachePath `
    -ComponentIds "vcredist" `
    -Offline:$OfflineDependencies

dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "Release tool restore failed (exit code $LASTEXITCODE)."
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $resolvedOutput `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=false `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    throw "Portable Developer publish failed (exit code $LASTEXITCODE)."
}

& (Join-Path $PSScriptRoot "Bundle-PortableNativeRuntime.ps1") `
    -OutputPath $resolvedOutput `
    -DependencyCatalogPath $DependencyCatalogPath `
    -DependencyCachePath $DependencyCachePath

& (Join-Path $PSScriptRoot "Test-ReleaseMetadata.ps1") `
    -ExecutablePath (Join-Path $resolvedOutput "PortableDeveloper.exe") `
    -Version $Version

foreach ($document in @("LICENSE", "PRIVACY.md", "THIRD-PARTY-NOTICES.md")) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $document) -Destination (Join-Path $resolvedOutput $document)
}

$unsignedNotice = @"
Portable Developer $Version is free software licensed under GPL-3.0-or-later.

This release is currently NOT digitally signed. Windows Smart App Control or
Microsoft Defender SmartScreen may therefore block it. Do not disable Windows
security protections solely to run this application. Future releases are planned
to use public code signing through SignPath Foundation.

The application downloads modules only after an explicit user action and accepts
them only when their HTTPS source and SHA-256 match the catalog shipped here.
"@
$unsignedNotice | Set-Content -LiteralPath (Join-Path $resolvedOutput "UNSIGNED-BUILD.txt") -Encoding utf8

$dependencyLockHash = (Get-FileHash -LiteralPath (Join-Path $resolvedOutput "catalog\dependencies.lock.json") -Algorithm SHA256).Hash.ToLowerInvariant()
[ordered]@{
    schemaVersion = 1
    version = $Version
    runtime = "win-x64"
    selfContained = $true
    moduleDelivery = "verified-runtime-download"
    digitallySigned = $false
    dependencyLockSha256 = $dependencyLockHash
    createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $resolvedOutput "release-manifest.json") -Encoding utf8

$archivePath = "$resolvedOutput.zip"
$checksumPath = "$archivePath.sha256"
if ((Test-Path -LiteralPath $archivePath) -or (Test-Path -LiteralPath $checksumPath)) {
    throw "Release archive target already exists."
}

Compress-Archive -LiteralPath $resolvedOutput -DestinationPath $archivePath -CompressionLevel Optimal
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$archiveHash  $([System.IO.Path]::GetFileName($archivePath))" | Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Host "Portable online release: $resolvedOutput"
Write-Host "Release archive: $archivePath"
Write-Host "SHA-256: $archiveHash"

& (Join-Path $PSScriptRoot "Cleanup-Releases.ps1") -PublishRoot $publishRoot -Keep $ReleasesToKeep
