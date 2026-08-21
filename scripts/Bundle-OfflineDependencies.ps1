[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$LaragonBinPath = "E:\laragon\bin",

    [string]$MariaDbArchivePath = (Join-Path $PSScriptRoot "..\downloads\mariadb-12.3.2-winx64.zip"),

    [string]$SeleniumServerPath = (Join-Path $PSScriptRoot "..\downloads\bundle-cache\selenium-server-4.47.0.jar"),

    [string]$NativeRuntimePath = "$env:SystemRoot\System32"
)

$ErrorActionPreference = "Stop"

$apacheVersion = "2.4.66"
$phpVersion = "8.4.12"
$mariaDbVersion = "12.3.2"
$seleniumVersion = "4.47.0"
$javaVersion = "25.0.3"
$composerVersion = "2.9.4"
$mariaDbArchiveSha256 = "67347c129eb9c5923d002ea34fbfa27c60eb95d36dd73b85af2651cdeceecac5"
$runtimeFileNames = @(
    "vcruntime140.dll",
    "vcruntime140_1.dll",
    "msvcp140.dll",
    "msvcp140_1.dll",
    "msvcp140_2.dll",
    "msvcp140_atomic_wait.dll",
    "msvcp140_codecvt_ids.dll"
)

function Resolve-RequiredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found: $Path"
    }

    return [System.IO.Path]::GetFullPath($Path)
}

function Assert-Sha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Expected
    )

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Expected.ToLowerInvariant()) {
        throw "SHA-256 mismatch for $Path. Expected $Expected, got $actual."
    }
}

function Copy-ModuleDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        throw "Bundle destination already exists: $Destination"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

function Write-ModuleMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [object]$CatalogItem,

        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot
    )

    $entrypointPath = Join-Path $ModuleRoot $CatalogItem.entrypointRelativePath
    Assert-Sha256 -Path $entrypointPath -Expected $CatalogItem.entrypointSha256
    $metadata = [ordered]@{
        kind = $CatalogItem.kind
        version = $CatalogItem.version
        sourceUrl = $CatalogItem.sourceUrl
        entrypointSha256 = $CatalogItem.entrypointSha256
        entrypointRelativePath = $CatalogItem.entrypointRelativePath
    }
    $metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $ModuleRoot ".portable-developer-module.json") -Encoding utf8
}

function Copy-NativeRuntime {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Destination,

        [Parameter(Mandatory = $true)]
        [string[]]$RequiredMetadataFiles
    )

    $metadata = @()
    foreach ($fileName in $runtimeFileNames) {
        $sourcePath = Resolve-RequiredPath -Path (Join-Path $NativeRuntimePath $fileName) -Description "Microsoft Visual C++ runtime file"
        $file = Get-Item -LiteralPath $sourcePath
        $version = [Version]$file.VersionInfo.FileVersion
        if ($version -lt [Version]"14.50.0.0") {
            throw "Microsoft runtime $fileName is too old: $version"
        }

        $signature = Get-AuthenticodeSignature -LiteralPath $sourcePath
        if ($signature.Status -ne "Valid" -or $signature.SignerCertificate.Subject -notmatch "O=Microsoft Corporation") {
            throw "Microsoft runtime signature is not trusted: $sourcePath"
        }

        $destinationPath = Join-Path $Destination $fileName
        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
        if ($fileName -in $RequiredMetadataFiles) {
            $metadata += [ordered]@{
                fileName = $fileName
                fileVersion = $version.ToString()
                sha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash.ToLowerInvariant()
                signer = "Microsoft Corporation"
                importedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
            }
        }
    }

    $moduleRoot = if ((Split-Path -Leaf $Destination) -eq "bin") { Split-Path -Parent $Destination } else { $Destination }
    ConvertTo-Json -InputObject @($metadata) | Set-Content -LiteralPath (Join-Path $moduleRoot ".portable-developer-runtime.json") -Encoding utf8
}

$resolvedOutput = Resolve-RequiredPath -Path $OutputPath -Description "Published application directory"
$resolvedLaragonBin = Resolve-RequiredPath -Path $LaragonBinPath -Description "Laragon bin directory"
$resolvedMariaDbArchive = Resolve-RequiredPath -Path $MariaDbArchivePath -Description "MariaDB ZIP archive"
$resolvedSeleniumServer = Resolve-RequiredPath -Path $SeleniumServerPath -Description "Selenium Server JAR"
$resolvedNativeRuntime = Resolve-RequiredPath -Path $NativeRuntimePath -Description "Microsoft runtime source directory"
$NativeRuntimePath = $resolvedNativeRuntime

$catalogPath = Resolve-RequiredPath -Path (Join-Path $PSScriptRoot "..\catalog\modules.json") -Description "Bundled module catalog"
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$catalogByKind = @{}
foreach ($item in $catalog.packages) {
    $catalogByKind[$item.kind] = $item
}

$apacheSource = Resolve-RequiredPath -Path (Join-Path $resolvedLaragonBin "apache\httpd-2.4.66-260223-Win64-VS18") -Description "Laragon Apache $apacheVersion"
$phpSource = Resolve-RequiredPath -Path (Join-Path $resolvedLaragonBin "php\php-8.4.12-nts-Win32-vs17-x64") -Description "Laragon PHP $phpVersion"
$javaSource = Resolve-RequiredPath -Path (Join-Path $resolvedLaragonBin "dbeaver\jre") -Description "Laragon bundled Microsoft OpenJDK $javaVersion"
$composerSource = Resolve-RequiredPath -Path (Join-Path $resolvedLaragonBin "composer\composer.phar") -Description "Laragon Composer $composerVersion"

Assert-Sha256 -Path $resolvedMariaDbArchive -Expected $mariaDbArchiveSha256
Assert-Sha256 -Path $resolvedSeleniumServer -Expected $catalogByKind.selenium.entrypointSha256

$modulesRoot = Join-Path $resolvedOutput "modules"
New-Item -ItemType Directory -Path $modulesRoot -Force | Out-Null

$apacheTarget = Join-Path $modulesRoot "apache\$apacheVersion"
$phpTarget = Join-Path $modulesRoot "php\$phpVersion"
$mariaDbTarget = Join-Path $modulesRoot "mariadb\$mariaDbVersion"
$seleniumTarget = Join-Path $modulesRoot "selenium\$seleniumVersion"
$javaTarget = Join-Path $modulesRoot "jre\$javaVersion"
$composerTarget = Join-Path $modulesRoot "composer\$composerVersion"

Copy-ModuleDirectory -Source $apacheSource -Destination $apacheTarget
Copy-ModuleDirectory -Source $phpSource -Destination $phpTarget
Copy-ModuleDirectory -Source $javaSource -Destination $javaTarget
New-Item -ItemType Directory -Path $composerTarget -Force | Out-Null
Copy-Item -LiteralPath $composerSource -Destination (Join-Path $composerTarget "composer.phar")

$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
$mariaDbExtraction = Join-Path $temporaryRoot ("PortableDeveloperBundle-MariaDb-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $mariaDbExtraction | Out-Null
try {
    Expand-Archive -LiteralPath $resolvedMariaDbArchive -DestinationPath $mariaDbExtraction
    $mariaDbSource = Resolve-RequiredPath -Path (Join-Path $mariaDbExtraction "mariadb-12.3.2-winx64") -Description "Extracted MariaDB root"
    Copy-ModuleDirectory -Source $mariaDbSource -Destination $mariaDbTarget
}
finally {
    if (Test-Path -LiteralPath $mariaDbExtraction) {
        $resolvedExtraction = [System.IO.Path]::GetFullPath($mariaDbExtraction)
        $expectedPrefix = $temporaryRoot + [System.IO.Path]::DirectorySeparatorChar + "PortableDeveloperBundle-MariaDb-"
        if (-not $resolvedExtraction.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected MariaDB staging path: $resolvedExtraction"
        }

        Remove-Item -LiteralPath $mariaDbExtraction -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $seleniumTarget -Force | Out-Null
Copy-Item -LiteralPath $resolvedSeleniumServer -Destination (Join-Path $seleniumTarget "selenium-server.jar")

Copy-NativeRuntime -Destination (Join-Path $apacheTarget "bin") -RequiredMetadataFiles @("vcruntime140.dll")
Copy-NativeRuntime -Destination $phpTarget -RequiredMetadataFiles @("vcruntime140.dll", "vcruntime140_1.dll")

Write-ModuleMetadata -CatalogItem $catalogByKind.apache -ModuleRoot $apacheTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.php -ModuleRoot $phpTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.mariaDb -ModuleRoot $mariaDbTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.selenium -ModuleRoot $seleniumTarget

$bundleManifest = [ordered]@{
    schemaVersion = 1
    createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    components = @(
        [ordered]@{ name = "Apache"; version = $apacheVersion; source = $catalogByKind.apache.sourceUrl; entrypointSha256 = $catalogByKind.apache.entrypointSha256 }
        [ordered]@{ name = "PHP"; version = $phpVersion; source = $catalogByKind.php.sourceUrl; entrypointSha256 = $catalogByKind.php.entrypointSha256 }
        [ordered]@{ name = "MariaDB"; version = $mariaDbVersion; source = $catalogByKind.mariaDb.sourceUrl; entrypointSha256 = $catalogByKind.mariaDb.entrypointSha256 }
        [ordered]@{ name = "Selenium Server"; version = $seleniumVersion; source = $catalogByKind.selenium.sourceUrl; entrypointSha256 = $catalogByKind.selenium.entrypointSha256 }
        [ordered]@{ name = "Microsoft OpenJDK Runtime"; version = $javaVersion; source = "https://learn.microsoft.com/java/openjdk/" }
        [ordered]@{ name = "Composer"; version = $composerVersion; source = "https://getcomposer.org/"; sha256 = (Get-FileHash -LiteralPath (Join-Path $composerTarget "composer.phar") -Algorithm SHA256).Hash.ToLowerInvariant() }
    )
}
$bundleManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $resolvedOutput "bundle-manifest.json") -Encoding utf8

Write-Host "Offline dependencies bundled into: $resolvedOutput"
