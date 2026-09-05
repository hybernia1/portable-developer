[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$DependencyCatalogPath = (Join-Path $PSScriptRoot "..\catalog\dependencies.lock.json"),

    [string]$DependencyCachePath = (Join-Path $PSScriptRoot "..\downloads\dependencies")
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "VerifiedVcRuntimeExtraction.ps1")

$runtimeFileNames = @(
    "vcruntime140.dll",
    "vcruntime140_1.dll",
    "msvcp140.dll",
    "msvcp140_1.dll",
    "msvcp140_2.dll",
    "msvcp140_atomic_wait.dll",
    "msvcp140_codecvt_ids.dll"
)

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Container)) {
    throw "Published application directory was not found: $resolvedOutput"
}

$catalog = Get-Content -LiteralPath $DependencyCatalogPath -Raw | ConvertFrom-Json
$runtime = @($catalog.components | Where-Object { $_.id -eq "vcredist" })
if ($runtime.Count -ne 1 -or $null -eq $runtime[0].runtimeFiles) {
    throw "The dependency lock must contain exactly one VC++ runtime definition."
}

$runtime = $runtime[0]
$installer = [System.IO.Path]::GetFullPath((Join-Path $DependencyCachePath (Join-Path $runtime.id (Join-Path $runtime.version $runtime.fileName))))
if (-not (Test-Path -LiteralPath $installer -PathType Leaf) -or (Get-Sha256 $installer) -ne $runtime.archiveSha256.ToLowerInvariant()) {
    throw "The verified VC++ Redistributable cache file is missing or invalid: $installer"
}

$temporaryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\temp\package-builds")).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar)
New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
$staging = Join-Path $temporaryRoot ("PortableDeveloperNativeRuntime-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    $expanded = Join-Path $staging "expanded"
    Expand-PdVerifiedVcRuntime `
        -InstallerPath $installer `
        -ExpectedInstallerSha256 $runtime.archiveSha256 `
        -ExpectedSignerSubjectContains $runtime.signerSubjectContains `
        -ExpectedVersion $runtime.version `
        -RuntimeFiles $runtime.runtimeFiles `
        -StagingRoot $staging `
        -OutputPath $expanded

    $target = Join-Path $resolvedOutput (Join-Path "runtime\vcredist" $runtime.version)
    if (Test-Path -LiteralPath $target) {
        throw "Portable native runtime target already exists: $target"
    }

    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $manifestFiles = @()
    foreach ($fileName in $runtimeFileNames) {
        $source = Join-Path $expanded $fileName
        $expectedHash = $runtime.runtimeFiles.PSObject.Properties[$fileName].Value
        if (-not (Test-Path -LiteralPath $source -PathType Leaf) -or (Get-Sha256 $source) -ne $expectedHash.ToLowerInvariant()) {
            throw "Extracted VC++ runtime file failed SHA-256 verification: $fileName"
        }

        $signature = Get-AuthenticodeSignature -LiteralPath $source
        if ($signature.Status -ne "Valid" -or $signature.SignerCertificate.Subject -notmatch "O=Microsoft Corporation") {
            throw "Extracted VC++ runtime file has an invalid signature: $fileName"
        }

        Copy-Item -LiteralPath $source -Destination (Join-Path $target $fileName)
        $manifestFiles += [ordered]@{
            fileName = $fileName
            sha256 = $expectedHash
            fileVersion = (Get-Item -LiteralPath $source).VersionInfo.FileVersion
            signer = "Microsoft Corporation"
        }
    }

    [ordered]@{
        schemaVersion = 1
        version = $runtime.version
        source = $runtime.sources[0]
        archiveSha256 = $runtime.archiveSha256
        files = $manifestFiles
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $target "runtime-manifest.json") -Encoding utf8
}
finally {
    if (Test-Path -LiteralPath $staging) {
        $resolvedStaging = [System.IO.Path]::GetFullPath($staging)
        $expectedPrefix = $temporaryRoot + [System.IO.Path]::DirectorySeparatorChar + "PortableDeveloperNativeRuntime-"
        if (-not $resolvedStaging.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected native runtime staging path: $resolvedStaging"
        }

        [System.IO.Directory]::Delete($resolvedStaging, $true)
    }
}

Write-Host "Portable VC++ runtime bundled into: $resolvedOutput"
