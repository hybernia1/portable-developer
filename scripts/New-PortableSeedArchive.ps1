[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$SourceRevision,

    [string]$DependencyCatalogPath = (Join-Path $PSScriptRoot "..\catalog\dependencies.lock.json"),

    [string]$DependencyCachePath = (Join-Path $PSScriptRoot "..\downloads\dependencies")
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$temporaryRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "temp\package-builds")).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar)
$resolvedArchive = [System.IO.Path]::GetFullPath($ArchivePath)
if (-not $resolvedArchive.StartsWith($temporaryRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Portable seed archives must remain below: $temporaryRoot"
}

if (Test-Path -LiteralPath $resolvedArchive) {
    throw "Portable seed archive already exists: $resolvedArchive"
}

New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
$staging = Join-Path $temporaryRoot ("PortableDeveloperSeed-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    $catalogTarget = Join-Path $staging "catalog"
    New-Item -ItemType Directory -Path $catalogTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "catalog\modules.json") -Destination $catalogTarget
    Copy-Item -LiteralPath $DependencyCatalogPath -Destination (Join-Path $catalogTarget "dependencies.lock.json")

    $seleniumTarget = Join-Path $staging "resources\selenium"
    $logosTarget = Join-Path $staging "resources\logos"
    New-Item -ItemType Directory -Path $seleniumTarget -Force | Out-Null
    New-Item -ItemType Directory -Path $logosTarget -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "src\PortableDeveloper.App\Resources\Selenium\PortableProfileNode.java") -Destination $seleniumTarget
    Copy-Item -Path (Join-Path $repositoryRoot "src\PortableDeveloper.App\Assets\Logos\*.svg") -Destination $logosTarget

    & (Join-Path $PSScriptRoot "Bundle-PortableNativeRuntime.ps1") `
        -OutputPath $staging `
        -DependencyCatalogPath $DependencyCatalogPath `
        -DependencyCachePath $DependencyCachePath

    $releaseDocumentsPath = Join-Path $staging "docs"
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

    $dependencyLockHash = (Get-FileHash -LiteralPath (Join-Path $catalogTarget "dependencies.lock.json") -Algorithm SHA256).Hash.ToLowerInvariant()
    [ordered]@{
        schemaVersion = 1
        version = $Version
        runtime = "win-x64"
        selfContained = $true
        sourceRepository = "https://github.com/hybernia1/portable-developer"
        sourceRevision = $SourceRevision.ToLowerInvariant()
        moduleDelivery = "verified-runtime-download"
        digitallySigned = $false
        dependencyLockSha256 = $dependencyLockHash
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releaseDocumentsPath "release-manifest.json") -Encoding utf8

    $rootPrefix = $staging.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $manifestFiles = @(
        Get-ChildItem -LiteralPath $staging -File -Recurse |
            Sort-Object FullName |
            ForEach-Object {
                $relativePath = $_.FullName.Substring($rootPrefix.Length).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
                [ordered]@{
                    path = $relativePath
                    length = $_.Length
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            })
    if ($manifestFiles.Count -eq 0 -or $manifestFiles.Count -ge 128) {
        throw "Portable seed has an invalid file count: $($manifestFiles.Count)"
    }

    $manifestJson = [ordered]@{
        schemaVersion = 1
        version = $Version
        files = $manifestFiles
    } | ConvertTo-Json -Depth 5
    [System.IO.File]::WriteAllText(
        (Join-Path $staging "portable-seed-manifest.json"),
        $manifestJson,
        [System.Text.UTF8Encoding]::new($false))

    $archiveDirectory = [System.IO.Path]::GetDirectoryName($resolvedArchive)
    New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archiveStream = [System.IO.File]::Open(
        $resolvedArchive,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $archiveStream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $true)
        try {
            foreach ($file in @(Get-ChildItem -LiteralPath $staging -File -Recurse | Sort-Object FullName)) {
                $relativePath = $file.FullName.Substring($rootPrefix.Length).Replace(
                    [System.IO.Path]::DirectorySeparatorChar,
                    '/')
                $entry = $archive.CreateEntry(
                    $relativePath,
                    [System.IO.Compression.CompressionLevel]::Optimal)
                $entryStream = $entry.Open()
                $sourceStream = [System.IO.File]::OpenRead($file.FullName)
                try {
                    $sourceStream.CopyTo($entryStream)
                }
                finally {
                    $sourceStream.Dispose()
                    $entryStream.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $archiveStream.Dispose()
    }

    $verificationArchive = [System.IO.Compression.ZipFile]::OpenRead($resolvedArchive)
    try {
        $invalidEntry = @($verificationArchive.Entries | Where-Object { $_.FullName.Contains('\') }) |
            Select-Object -First 1
        if ($null -ne $invalidEntry) {
            throw "Portable seed archive contains a non-portable entry path: $($invalidEntry.FullName)"
        }
    }
    finally {
        $verificationArchive.Dispose()
    }
}
finally {
    if (Test-Path -LiteralPath $staging -PathType Container) {
        $resolvedStaging = [System.IO.Path]::GetFullPath($staging)
        $expectedPrefix = $temporaryRoot + [System.IO.Path]::DirectorySeparatorChar + "PortableDeveloperSeed-"
        if (-not $resolvedStaging.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected portable seed staging path: $resolvedStaging"
        }

        [System.IO.Directory]::Delete($resolvedStaging, $true)
    }
}

Write-Host "Portable seed archive created: $resolvedArchive"
