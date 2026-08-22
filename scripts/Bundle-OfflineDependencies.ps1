[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$LaragonBinPath = "E:\laragon\bin",

    [string]$PhpMyAdminPath = "E:\laragon\etc\apps\phpmyadmin",

    [string]$MariaDbArchivePath = (Join-Path $PSScriptRoot "..\downloads\mariadb-12.3.2-winx64.zip"),

    [string]$SeleniumServerPath = (Join-Path $PSScriptRoot "..\downloads\bundle-cache\selenium-server-4.47.0.jar"),

    [string]$GeckoDriverArchivePath = (Join-Path $PSScriptRoot "..\downloads\bundle-cache\geckodriver-v0.37.1-win64.zip"),

    [string]$ComposerPath = (Join-Path $PSScriptRoot "..\downloads\bundle-cache\composer-2.10.2.phar"),

    [string]$NativeRuntimePath = "$env:SystemRoot\System32"
)

$ErrorActionPreference = "Stop"

$apacheVersion = "2.4.66"
$phpVersion = "8.4.12"
$mariaDbVersion = "12.3.2"
$seleniumVersion = "4.47.0"
$geckoDriverVersion = "0.37.1"
$javaVersion = "25.0.3"
$composerVersion = "2.10.2"
$pythonVersion = "3.13.0"
$phpMyAdminVersion = "5.2.3"
$mariaDbArchiveSha256 = "67347c129eb9c5923d002ea34fbfa27c60eb95d36dd73b85af2651cdeceecac5"
$geckoDriverArchiveSha256 = "dfed9315abe8d2fbc1b6161a2ee8002452e79cf05ee92fdc653a4e26bc35edd8"
$composerSha256 = "5ee7125f8a30a34d246cefdc0bc85b8a783b28f2aec968994118512350d28027"
$pythonEntrypointSha256 = "62ebc90a2884bb63a0cd67e789cafdd51e771eee043587e2354327b4ccc9bb05"
$phpMyAdminComposerLockSha256 = "ab897b93490b7e7a8df687aa40f72a9467e4d0b9d6395f46071604d6ca1cd333"
$phpMyAdminReleaseMarkerSha256 = "b0397dbc63b97792ee1a42357a83e97810aba27c9f571a1c017f8aaf5f8d1fe0"
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

function Copy-PythonRuntime {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        throw "Python destination already exists: $Destination"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    foreach ($item in Get-ChildItem -LiteralPath $Source -Force) {
        if ($item.Name -eq "Scripts") {
            continue
        }

        if ($item.Name -eq "Lib") {
            $libTarget = Join-Path $Destination "Lib"
            New-Item -ItemType Directory -Path $libTarget -Force | Out-Null
            Get-ChildItem -LiteralPath $item.FullName -Force |
                Where-Object { $_.Name -ne "site-packages" } |
                Copy-Item -Destination $libTarget -Recurse -Force
            continue
        }

        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Path (Join-Path $Destination "Lib\site-packages") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination "Scripts") -Force | Out-Null
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

function Write-ToolMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Kind,

        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot,

        [Parameter(Mandatory = $true)]
        [string]$EntrypointRelativePath,

        [Parameter(Mandatory = $true)]
        [string]$EntrypointSha256
    )

    Assert-Sha256 -Path (Join-Path $ModuleRoot $EntrypointRelativePath) -Expected $EntrypointSha256
    $metadata = [ordered]@{
        schemaVersion = 1
        kind = $Kind
        version = $Version
        entrypointRelativePath = $EntrypointRelativePath
        entrypointSha256 = $EntrypointSha256
    }
    $metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $ModuleRoot ".portable-developer-tool.json") -Encoding utf8
}

function Copy-PhpMyAdmin {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        throw "phpMyAdmin destination already exists: $Destination"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $excludedNames = @("config.inc.php", "setup", "tmp")
    Get-ChildItem -LiteralPath $Source -Force |
        Where-Object { $_.Name -notin $excludedNames } |
        Copy-Item -Destination $Destination -Recurse -Force

    $bridge = @'
<?php
declare(strict_types=1);
$portableRoot = dirname(__DIR__, 3);
require $portableRoot . '/temp/generated/default/phpmyadmin/config.inc.php';
'@
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText(
        (Join-Path $Destination "config.inc.php"),
        $bridge,
        $utf8NoBom)
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
$resolvedPhpMyAdmin = Resolve-RequiredPath -Path $PhpMyAdminPath -Description "phpMyAdmin $phpMyAdminVersion directory"
$resolvedMariaDbArchive = Resolve-RequiredPath -Path $MariaDbArchivePath -Description "MariaDB ZIP archive"
$resolvedSeleniumServer = Resolve-RequiredPath -Path $SeleniumServerPath -Description "Selenium Server JAR"
$resolvedGeckoDriverArchive = Resolve-RequiredPath -Path $GeckoDriverArchivePath -Description "geckodriver Windows x64 ZIP"
$resolvedComposer = Resolve-RequiredPath -Path $ComposerPath -Description "Composer $composerVersion PHAR"
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
$pythonSource = Resolve-RequiredPath -Path (Join-Path $resolvedLaragonBin "python\python-3.13") -Description "Laragon Python $pythonVersion runtime"

Assert-Sha256 -Path $resolvedMariaDbArchive -Expected $mariaDbArchiveSha256
Assert-Sha256 -Path $resolvedSeleniumServer -Expected $catalogByKind.selenium.entrypointSha256
Assert-Sha256 -Path $resolvedGeckoDriverArchive -Expected $geckoDriverArchiveSha256
Assert-Sha256 -Path $resolvedComposer -Expected $composerSha256
Assert-Sha256 -Path (Join-Path $pythonSource "python.exe") -Expected $pythonEntrypointSha256
Assert-Sha256 -Path (Join-Path $resolvedPhpMyAdmin "composer.lock") -Expected $phpMyAdminComposerLockSha256
Assert-Sha256 -Path (Join-Path $resolvedPhpMyAdmin "RELEASE-DATE-5.2.3") -Expected $phpMyAdminReleaseMarkerSha256

$modulesRoot = Join-Path $resolvedOutput "modules"
New-Item -ItemType Directory -Path $modulesRoot -Force | Out-Null

$apacheTarget = Join-Path $modulesRoot "apache\$apacheVersion"
$phpTarget = Join-Path $modulesRoot "php\$phpVersion"
$mariaDbTarget = Join-Path $modulesRoot "mariadb\$mariaDbVersion"
$seleniumTarget = Join-Path $modulesRoot "selenium\$seleniumVersion"
$javaTarget = Join-Path $modulesRoot "jre\$javaVersion"
$composerTarget = Join-Path $modulesRoot "composer\$composerVersion"
$pythonTarget = Join-Path $modulesRoot "python\$pythonVersion"
$phpMyAdminTarget = Join-Path $resolvedOutput "tools\phpmyadmin\$phpMyAdminVersion"

Copy-ModuleDirectory -Source $apacheSource -Destination $apacheTarget
$apacheRuntimeArtifacts = @(
    "logs\access.log",
    "logs\error.log",
    "logs\access_log",
    "logs\error_log",
    "logs\httpd.pid"
)
foreach ($relativeArtifact in $apacheRuntimeArtifacts) {
    $artifactPath = Join-Path $apacheTarget $relativeArtifact
    if (Test-Path -LiteralPath $artifactPath -PathType Leaf) {
        Remove-Item -LiteralPath $artifactPath -Force
    }
}

Copy-ModuleDirectory -Source $phpSource -Destination $phpTarget
$sourcePhpIni = Join-Path $phpTarget "php.ini"
if (Test-Path -LiteralPath $sourcePhpIni -PathType Leaf) {
    Remove-Item -LiteralPath $sourcePhpIni -Force
}

Copy-ModuleDirectory -Source $javaSource -Destination $javaTarget
New-Item -ItemType Directory -Path $composerTarget -Force | Out-Null
Copy-Item -LiteralPath $resolvedComposer -Destination (Join-Path $composerTarget "composer.phar")
Copy-PythonRuntime -Source $pythonSource -Destination $pythonTarget
$pythonExecutable = Join-Path $pythonTarget "python.exe"
$previousPythonNoUserSite = $env:PYTHONNOUSERSITE
try {
    $env:PYTHONNOUSERSITE = "1"
    & $pythonExecutable -I -m ensurepip --upgrade --default-pip
    if ($LASTEXITCODE -ne 0) {
        throw "Bootstrapping the bundled Python pip failed (exit code $LASTEXITCODE)."
    }
}
finally {
    $env:PYTHONNOUSERSITE = $previousPythonNoUserSite
}

& $pythonExecutable -I -m pip --version --disable-pip-version-check
if ($LASTEXITCODE -ne 0) {
    throw "The bundled Python pip verification failed (exit code $LASTEXITCODE)."
}

Copy-PhpMyAdmin -Source $resolvedPhpMyAdmin -Destination $phpMyAdminTarget

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

$geckoDriverExtraction = Join-Path $temporaryRoot ("PortableDeveloperBundle-GeckoDriver-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $geckoDriverExtraction | Out-Null
try {
    Expand-Archive -LiteralPath $resolvedGeckoDriverArchive -DestinationPath $geckoDriverExtraction
    $geckoDriverSource = Resolve-RequiredPath -Path (Join-Path $geckoDriverExtraction "geckodriver.exe") -Description "Extracted geckodriver executable"
    $geckoDriverTarget = Join-Path $resolvedOutput "drivers\bundled\firefox\$geckoDriverVersion\geckodriver.exe"
    New-Item -ItemType Directory -Path (Split-Path -Parent $geckoDriverTarget) -Force | Out-Null
    Copy-Item -LiteralPath $geckoDriverSource -Destination $geckoDriverTarget
    $driverManifest = [ordered]@{
        schemaVersion = 1
        drivers = @(
            [ordered]@{
                browserName = "firefox"
                version = $geckoDriverVersion
                relativePath = "drivers/bundled/firefox/$geckoDriverVersion/geckodriver.exe"
                sha256 = (Get-FileHash -LiteralPath $geckoDriverTarget -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        )
    }
    $driverManifestPath = Join-Path $resolvedOutput "drivers\bundled\drivers.json"
    $driverManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $driverManifestPath -Encoding utf8
    New-Item -ItemType Directory -Path (Join-Path $resolvedOutput "drivers\custom") -Force | Out-Null
}
finally {
    if (Test-Path -LiteralPath $geckoDriverExtraction) {
        $resolvedExtraction = [System.IO.Path]::GetFullPath($geckoDriverExtraction)
        $expectedPrefix = $temporaryRoot + [System.IO.Path]::DirectorySeparatorChar + "PortableDeveloperBundle-GeckoDriver-"
        if (-not $resolvedExtraction.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected geckodriver staging path: $resolvedExtraction"
        }

        Remove-Item -LiteralPath $geckoDriverExtraction -Recurse -Force
    }
}

Copy-NativeRuntime -Destination (Join-Path $apacheTarget "bin") -RequiredMetadataFiles @("vcruntime140.dll")
Copy-NativeRuntime -Destination $phpTarget -RequiredMetadataFiles @("vcruntime140.dll", "vcruntime140_1.dll")

Write-ModuleMetadata -CatalogItem $catalogByKind.apache -ModuleRoot $apacheTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.php -ModuleRoot $phpTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.mariaDb -ModuleRoot $mariaDbTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.selenium -ModuleRoot $seleniumTarget
Write-ToolMetadata -Kind "composer" -Version $composerVersion -ModuleRoot $composerTarget -EntrypointRelativePath "composer.phar" -EntrypointSha256 $composerSha256
Write-ToolMetadata -Kind "python" -Version $pythonVersion -ModuleRoot $pythonTarget -EntrypointRelativePath "python.exe" -EntrypointSha256 $pythonEntrypointSha256

$resolvedOutputPrefix = $resolvedOutput.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
foreach ($debugSymbol in Get-ChildItem -LiteralPath $resolvedOutput -Recurse -Filter "*.pdb" -File) {
    $resolvedDebugSymbol = [System.IO.Path]::GetFullPath($debugSymbol.FullName)
    if (-not $resolvedDebugSymbol.StartsWith($resolvedOutputPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a debug symbol outside the release root: $resolvedDebugSymbol"
    }

    Remove-Item -LiteralPath $resolvedDebugSymbol -Force
}

$bundleManifest = [ordered]@{
    schemaVersion = 1
    createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    components = @(
        [ordered]@{ name = "Apache"; version = $apacheVersion; source = $catalogByKind.apache.sourceUrl; entrypointSha256 = $catalogByKind.apache.entrypointSha256 }
        [ordered]@{ name = "PHP"; version = $phpVersion; source = $catalogByKind.php.sourceUrl; entrypointSha256 = $catalogByKind.php.entrypointSha256 }
        [ordered]@{ name = "MariaDB"; version = $mariaDbVersion; source = $catalogByKind.mariaDb.sourceUrl; entrypointSha256 = $catalogByKind.mariaDb.entrypointSha256 }
        [ordered]@{ name = "Selenium Server"; version = $seleniumVersion; source = $catalogByKind.selenium.sourceUrl; entrypointSha256 = $catalogByKind.selenium.entrypointSha256 }
        [ordered]@{ name = "geckodriver"; version = $geckoDriverVersion; source = "https://github.com/mozilla/geckodriver/releases/tag/v0.37.1"; archiveSha256 = $geckoDriverArchiveSha256 }
        [ordered]@{ name = "Microsoft OpenJDK Runtime"; version = $javaVersion; source = "https://learn.microsoft.com/java/openjdk/" }
        [ordered]@{ name = "Composer"; version = $composerVersion; source = "https://getcomposer.org/download/"; sha256 = $composerSha256 }
        [ordered]@{ name = "Python"; version = $pythonVersion; source = "https://www.python.org/downloads/release/python-3130/"; entrypointSha256 = $pythonEntrypointSha256; pip = (& $pythonExecutable -I -m ensurepip --version) }
        [ordered]@{ name = "phpMyAdmin"; version = $phpMyAdminVersion; source = "https://www.phpmyadmin.net/files/5.2.3/"; composerLockSha256 = $phpMyAdminComposerLockSha256 }
    )
}
$bundleManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $resolvedOutput "bundle-manifest.json") -Encoding utf8

Write-Host "Offline dependencies bundled into: $resolvedOutput"
