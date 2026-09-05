[CmdletBinding()]
param(
    [string]$Version = "1.26.0",
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\artifacts\publish\PortableDeveloper-win-x64-$Version"),
    [string]$DependencyCatalogPath = (Join-Path $PSScriptRoot "..\catalog\dependencies.lock.json"),
    [string]$DependencyCachePath = (Join-Path $PSScriptRoot "..\downloads\dependencies"),
    [string]$SourceRevision = $env:GITHUB_SHA,
    [switch]$OfflineDependencies,
    [switch]$SingleExecutable,

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

$resolvedSourceRevision = $SourceRevision.Trim()
if ([string]::IsNullOrWhiteSpace($resolvedSourceRevision) -and (Test-Path -LiteralPath (Join-Path $repositoryRoot ".git"))) {
    $resolvedSourceRevision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "The source revision could not be resolved from Git."
    }
}
if ([string]::IsNullOrWhiteSpace($resolvedSourceRevision)) {
    $resolvedSourceRevision = "unavailable"
}
elseif ($resolvedSourceRevision -notmatch '^[0-9a-fA-F]{40,64}$') {
    throw "SourceRevision must be a full Git commit hash or left empty when Git metadata is unavailable."
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

$seedWorkspace = $null
try {
    $publishArguments = @(
        "publish",
        $projectPath,
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "--output", $resolvedOutput,
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:PublishTrimmed=false",
        "-p:DebugType=None",
        "-p:DebugSymbols=false")

    if ($SingleExecutable) {
        $seedWorkspace = Join-Path $repositoryRoot ("temp\package-builds\PortableDeveloperSeedArchive-" + [Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $seedWorkspace | Out-Null
        $seedArchive = Join-Path $seedWorkspace "portable-seed.zip"
        & (Join-Path $PSScriptRoot "New-PortableSeedArchive.ps1") `
            -ArchivePath $seedArchive `
            -Version $Version `
            -SourceRevision $resolvedSourceRevision `
            -DependencyCatalogPath $DependencyCatalogPath `
            -DependencyCachePath $DependencyCachePath
        $publishArguments += "-p:PortableSeedArchive=$seedArchive"
        $publishArguments += "-p:EnableCompressionInSingleFile=true"
    }

    & dotnet @publishArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Portable Developer publish failed (exit code $LASTEXITCODE)."
    }
}
finally {
    if ($null -ne $seedWorkspace -and (Test-Path -LiteralPath $seedWorkspace -PathType Container)) {
        $resolvedSeedWorkspace = [System.IO.Path]::GetFullPath($seedWorkspace)
        $expectedSeedPrefix = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "temp\package-builds")) +
            [System.IO.Path]::DirectorySeparatorChar + "PortableDeveloperSeedArchive-"
        if (-not $resolvedSeedWorkspace.StartsWith($expectedSeedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected portable seed workspace: $resolvedSeedWorkspace"
        }

        [System.IO.Directory]::Delete($resolvedSeedWorkspace, $true)
    }
}

if (-not $SingleExecutable) {
    & (Join-Path $PSScriptRoot "Bundle-PortableNativeRuntime.ps1") `
        -OutputPath $resolvedOutput `
        -DependencyCatalogPath $DependencyCatalogPath `
        -DependencyCachePath $DependencyCachePath
}

& (Join-Path $PSScriptRoot "Test-ReleaseMetadata.ps1") `
    -ExecutablePath (Join-Path $resolvedOutput "PortableDeveloper.exe") `
    -Version $Version

if (-not $SingleExecutable) {
    $releaseDocumentsPath = Join-Path $resolvedOutput "docs"
    New-Item -ItemType Directory -Path $releaseDocumentsPath -Force | Out-Null
    foreach ($document in @("LICENSE", "PRIVACY.md", "THIRD-PARTY-NOTICES.md")) {
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $document) -Destination (Join-Path $releaseDocumentsPath $document)
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
    $unsignedNotice | Set-Content -LiteralPath (Join-Path $releaseDocumentsPath "UNSIGNED-BUILD.txt") -Encoding utf8

    $dependencyLockHash = (Get-FileHash -LiteralPath (Join-Path $resolvedOutput "catalog\dependencies.lock.json") -Algorithm SHA256).Hash.ToLowerInvariant()
    [ordered]@{
        schemaVersion = 1
        version = $Version
        runtime = "win-x64"
        selfContained = $true
        sourceRepository = "https://github.com/hybernia1/portable-developer"
        sourceRevision = $resolvedSourceRevision.ToLowerInvariant()
        moduleDelivery = "verified-runtime-download"
        digitallySigned = $false
        dependencyLockSha256 = $dependencyLockHash
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releaseDocumentsPath "release-manifest.json") -Encoding utf8
}

& (Join-Path $PSScriptRoot "Test-ReleaseLayout.ps1") -OutputPath $resolvedOutput -SingleExecutable:$SingleExecutable

$artifactPath = if ($SingleExecutable) { "$resolvedOutput.exe" } else { "$resolvedOutput.zip" }
$checksumPath = "$artifactPath.sha256"
$sbomPath = "$resolvedOutput.spdx.json"
$sbomStage = Join-Path $publishRoot "PortableDeveloper-sbom-$Version"
if ((Test-Path -LiteralPath $artifactPath) -or
    (Test-Path -LiteralPath $checksumPath) -or
    (Test-Path -LiteralPath $sbomPath) -or
    (Test-Path -LiteralPath $sbomStage)) {
    throw "Release artifact target already exists."
}

New-Item -ItemType Directory -Path $sbomStage | Out-Null
try {
    dotnet sbom-tool generate `
        -b $resolvedOutput `
        -bc (Join-Path $repositoryRoot "src") `
        -m $sbomStage `
        -pn "Portable Developer" `
        -pv $Version `
        -ps "Portable Developer contributors" `
        -nsb "https://github.com/hybernia1/portable-developer" `
        -nsu "$Version-$resolvedSourceRevision" `
        -mi "SPDX:2.2" `
        -F false `
        -pm true `
        -V Information
    if ($LASTEXITCODE -ne 0) {
        throw "Release SBOM generation failed (exit code $LASTEXITCODE)."
    }

    $generatedSbom = Join-Path $sbomStage "_manifest\spdx_2.2\manifest.spdx.json"
    if (-not (Test-Path -LiteralPath $generatedSbom -PathType Leaf)) {
        throw "Generated SPDX SBOM was not found: $generatedSbom"
    }
    Move-Item -LiteralPath $generatedSbom -Destination $sbomPath
}
finally {
    if (Test-Path -LiteralPath $sbomStage -PathType Container) {
        [System.IO.Directory]::Delete("\\?\$sbomStage", $true)
    }
}

if ($SingleExecutable) {
    Copy-Item -LiteralPath (Join-Path $resolvedOutput "PortableDeveloper.exe") -Destination $artifactPath
}
else {
    Compress-Archive -LiteralPath $resolvedOutput -DestinationPath $artifactPath -CompressionLevel Optimal
}

$artifactHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$artifactHash  $([System.IO.Path]::GetFileName($artifactPath))" | Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Host "Portable online release: $resolvedOutput"
Write-Host "Release artifact: $artifactPath"
Write-Host "Release SBOM: $sbomPath"
Write-Host "SHA-256: $artifactHash"

& (Join-Path $PSScriptRoot "Cleanup-Releases.ps1") -PublishRoot $publishRoot -Keep $ReleasesToKeep
