[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$DependencyCatalogPath = (Join-Path $PSScriptRoot "..\catalog\dependencies.lock.json"),

    [string]$DependencyCachePath = (Join-Path $PSScriptRoot "..\downloads\dependencies")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$pythonEntrypointSha256 = "62ebc90a2884bb63a0cd67e789cafdd51e771eee043587e2354327b4ccc9bb05"
$editorEntrypointSha256 = "1d9bd05023264ba49484174f01382a9d9b912d48495397b10ac4b5b9a2a227e9"
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

function Copy-JavaRuntime {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        throw "Java destination already exists: $Destination"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    foreach ($item in Get-ChildItem -LiteralPath $Source -Force) {
        if ($item.Name -in @("include", "jmods", "demo", "man")) {
            continue
        }

        if ($item.Name -eq "lib") {
            $libTarget = Join-Path $Destination "lib"
            New-Item -ItemType Directory -Path $libTarget -Force | Out-Null
            Get-ChildItem -LiteralPath $item.FullName -Force |
                Where-Object { $_.Name -ne "src.zip" } |
                Copy-Item -Destination $libTarget -Recurse -Force
            continue
        }

        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse -Force
    }
}

function Copy-PortableEditor {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    if (Test-Path -LiteralPath $Destination) {
        throw "Editor destination already exists: $Destination"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $rootFiles = @(
        "notepad++.exe",
        "doLocalConf.xml",
        "langs.model.xml",
        "stylers.model.xml",
        "contextMenu.xml",
        "readme.txt",
        "change.log"
    )
    foreach ($fileName in $rootFiles) {
        $sourcePath = Resolve-RequiredPath -Path (Join-Path $Source $fileName) -Description "Notepad++ $fileName"
        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $Destination $fileName)
    }

    foreach ($directoryName in @("autoCompletion", "functionList", "userDefineLangs")) {
        $sourcePath = Resolve-RequiredPath -Path (Join-Path $Source $directoryName) -Description "Notepad++ $directoryName directory"
        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $Destination $directoryName) -Recurse
    }

    $localizationTarget = Join-Path $Destination "localization"
    New-Item -ItemType Directory -Path $localizationTarget -Force | Out-Null
    Copy-Item `
        -LiteralPath (Resolve-RequiredPath -Path (Join-Path $Source "localization\czech.xml") -Description "Notepad++ Czech localization") `
        -Destination (Join-Path $localizationTarget "czech.xml")
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
        $expectedHash = $vcRuntimeHashes.PSObject.Properties[$fileName].Value
        Assert-Sha256 -Path $sourcePath -Expected $expectedHash
        $file = Get-Item -LiteralPath $sourcePath
        $version = [Version]$file.VersionInfo.FileVersion
        if ($version -ne [Version]$vcRedistVersion) {
            throw "Microsoft runtime $fileName has version $version, expected $vcRedistVersion."
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

function Test-CabinetFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $header = New-Object byte[] 4
        if ($stream.Read($header, 0, 4) -ne 4) {
            return $false
        }

        return [System.Text.Encoding]::ASCII.GetString($header) -eq "MSCF"
    }
    finally {
        $stream.Dispose()
    }
}

$resolvedOutput = Resolve-RequiredPath -Path $OutputPath -Description "Published application directory"
$resolvedDependencyCatalog = Resolve-RequiredPath -Path $DependencyCatalogPath -Description "Dependency lock"
$resolvedDependencyCache = Resolve-RequiredPath -Path $DependencyCachePath -Description "Verified dependency cache"
$dependencyCatalog = Get-Content -LiteralPath $resolvedDependencyCatalog -Raw | ConvertFrom-Json
if ($dependencyCatalog.schemaVersion -ne 1) {
    throw "Unsupported dependency lock schema: $resolvedDependencyCatalog"
}

$dependencies = @{}
foreach ($dependency in $dependencyCatalog.components) {
    $dependencies[$dependency.id] = $dependency
}

foreach ($requiredId in @("apache", "php", "mariadb", "selenium", "openjdk", "composer", "node", "python", "notepadpp", "phpmyadmin", "vcredist")) {
    if (-not $dependencies.ContainsKey($requiredId)) {
        throw "Dependency lock is missing required component: $requiredId"
    }
}

function Resolve-DependencyFile {
    param([Parameter(Mandatory = $true)][string]$Id)

    $dependency = $dependencies[$Id]
    $path = Join-Path $resolvedDependencyCache (Join-Path $dependency.id (Join-Path $dependency.version $dependency.fileName))
    $resolved = Resolve-RequiredPath -Path $path -Description "$($dependency.displayName) $($dependency.version) cache file"
    Assert-Sha256 -Path $resolved -Expected $dependency.archiveSha256
    return $resolved
}

$apacheVersion = $dependencies.apache.version
$phpVersion = $dependencies.php.version
$mariaDbVersion = $dependencies.mariadb.version
$seleniumVersion = $dependencies.selenium.version
$javaVersion = $dependencies.openjdk.version
$composerVersion = $dependencies.composer.version
$nodeVersion = $dependencies.node.version
$pythonVersion = $dependencies.python.version
$editorVersion = $dependencies.notepadpp.version
$phpMyAdminVersion = $dependencies.phpmyadmin.version
$vcRedistVersion = $dependencies.vcredist.version
$vcRuntimeHashes = $dependencies.vcredist.runtimeFiles
$mariaDbArchiveSha256 = $dependencies.mariadb.archiveSha256
$composerSha256 = $dependencies.composer.archiveSha256

$resolvedApacheArchive = Resolve-DependencyFile -Id "apache"
$resolvedPhpArchive = Resolve-DependencyFile -Id "php"
$resolvedMariaDbArchive = Resolve-DependencyFile -Id "mariadb"
$resolvedSeleniumServer = Resolve-DependencyFile -Id "selenium"
$resolvedJavaArchive = Resolve-DependencyFile -Id "openjdk"
$resolvedComposer = Resolve-DependencyFile -Id "composer"
$resolvedNodeArchive = Resolve-DependencyFile -Id "node"
$resolvedPythonArchive = Resolve-DependencyFile -Id "python"
$resolvedEditorArchive = Resolve-DependencyFile -Id "notepadpp"
$resolvedPhpMyAdminArchive = Resolve-DependencyFile -Id "phpmyadmin"
$resolvedVcRedist = Resolve-DependencyFile -Id "vcredist"

$vcInstallerSignature = Get-AuthenticodeSignature -LiteralPath $resolvedVcRedist
if ($vcInstallerSignature.Status -ne "Valid" -or
    $vcInstallerSignature.SignerCertificate.Subject -notmatch $dependencies.vcredist.signerSubjectContains) {
    throw "Microsoft Visual C++ Redistributable signature is not trusted: $resolvedVcRedist"
}

$catalogPath = Resolve-RequiredPath -Path (Join-Path $PSScriptRoot "..\catalog\modules.json") -Description "Bundled module catalog"
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$catalogByKind = @{}
foreach ($item in $catalog.packages) {
    $catalogByKind[$item.kind] = $item
}

$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
$dependencyExtraction = Join-Path $temporaryRoot ("PortableDeveloperBundle-Dependencies-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $dependencyExtraction | Out-Null
try {
    foreach ($archive in @(
        @{ Id = "apache"; Path = $resolvedApacheArchive },
        @{ Id = "php"; Path = $resolvedPhpArchive },
        @{ Id = "openjdk"; Path = $resolvedJavaArchive },
        @{ Id = "node"; Path = $resolvedNodeArchive },
        @{ Id = "python"; Path = $resolvedPythonArchive },
        @{ Id = "notepadpp"; Path = $resolvedEditorArchive },
        @{ Id = "phpmyadmin"; Path = $resolvedPhpMyAdminArchive }
    )) {
        [System.IO.Compression.ZipFile]::ExtractToDirectory(
            $archive.Path,
            (Join-Path $dependencyExtraction $archive.Id))
    }

    $apacheSource = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "apache\$($dependencies.apache.archiveRoot)") -Description "Extracted Apache $apacheVersion"
    $phpSource = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "php") -Description "Extracted PHP $phpVersion"
    $javaSource = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "openjdk\$($dependencies.openjdk.archiveRoot)") -Description "Extracted Microsoft OpenJDK $javaVersion"
    $nodeSource = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "node\$($dependencies.node.archiveRoot)") -Description "Extracted Node.js $nodeVersion"
    $pythonSource = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "python\$($dependencies.python.archiveRoot)") -Description "Extracted Python $pythonVersion"
    $editorSource = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "notepadpp") -Description "Extracted Notepad++ $editorVersion"
    $resolvedPhpMyAdmin = Resolve-RequiredPath -Path (Join-Path $dependencyExtraction "phpmyadmin\$($dependencies.phpmyadmin.archiveRoot)") -Description "Extracted phpMyAdmin $phpMyAdminVersion"

    $vcBundleExtraction = Join-Path $dependencyExtraction "vcredist-bundle"
    & dotnet tool run wix -- burn extract $resolvedVcRedist -o $vcBundleExtraction
    if ($LASTEXITCODE -ne 0) {
        throw "Extracting Microsoft Visual C++ Redistributable failed (exit code $LASTEXITCODE)."
    }

    $runtimeCabinet = $null
    foreach ($candidate in Get-ChildItem -LiteralPath $vcBundleExtraction -File) {
        if (-not (Test-CabinetFile -Path $candidate.FullName)) {
            continue
        }

        $listing = (& expand.exe -D $candidate.FullName 2>&1) -join "`n"
        if ($listing -match "vcruntime140\.dll_amd64") {
            $runtimeCabinet = $candidate.FullName
            break
        }
    }

    if ($null -eq $runtimeCabinet) {
        throw "The x64 Visual C++ runtime cabinet was not found in the Microsoft bundle."
    }

    $runtimeExtraction = Join-Path $dependencyExtraction "vcredist-runtime"
    $NativeRuntimePath = Join-Path $dependencyExtraction "vcredist-normalized"
    New-Item -ItemType Directory -Path $runtimeExtraction, $NativeRuntimePath -Force | Out-Null
    & expand.exe $runtimeCabinet -F:* $runtimeExtraction | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Extracting the x64 Visual C++ runtime cabinet failed (exit code $LASTEXITCODE)."
    }

    foreach ($fileName in $runtimeFileNames) {
        $sourceName = "${fileName}_amd64"
        $sourcePath = Resolve-RequiredPath -Path (Join-Path $runtimeExtraction $sourceName) -Description "Extracted Microsoft runtime $fileName"
        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $NativeRuntimePath $fileName)
    }

    Assert-Sha256 -Path (Join-Path $pythonSource "python.exe") -Expected $pythonEntrypointSha256
    Assert-Sha256 -Path (Join-Path $nodeSource "node.exe") -Expected $dependencies.node.normalizedEntrypointSha256
    Assert-Sha256 -Path (Join-Path $editorSource "notepad++.exe") -Expected $editorEntrypointSha256
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
$nodeTarget = Join-Path $modulesRoot "node\$nodeVersion"
$pythonTarget = Join-Path $modulesRoot "python\$pythonVersion"
$editorTarget = Join-Path $modulesRoot "editor\$editorVersion"
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
Get-ChildItem -LiteralPath $phpTarget -File -Filter "php.ini*" | ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Force
}

Copy-JavaRuntime -Source $javaSource -Destination $javaTarget
New-Item -ItemType Directory -Path $composerTarget -Force | Out-Null
Copy-Item -LiteralPath $resolvedComposer -Destination (Join-Path $composerTarget "composer.phar")
Copy-ModuleDirectory -Source $nodeSource -Destination $nodeTarget
Copy-PythonRuntime -Source $pythonSource -Destination $pythonTarget
Copy-PortableEditor -Source $editorSource -Destination $editorTarget
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

New-Item -ItemType Directory -Path (Join-Path $resolvedOutput "drivers\bundled") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $resolvedOutput "drivers\custom") -Force | Out-Null

Copy-NativeRuntime -Destination (Join-Path $apacheTarget "bin") -RequiredMetadataFiles @("vcruntime140.dll")
Copy-NativeRuntime -Destination $phpTarget -RequiredMetadataFiles @("vcruntime140.dll", "vcruntime140_1.dll")

Write-ModuleMetadata -CatalogItem $catalogByKind.apache -ModuleRoot $apacheTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.php -ModuleRoot $phpTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.mariaDb -ModuleRoot $mariaDbTarget
Write-ModuleMetadata -CatalogItem $catalogByKind.selenium -ModuleRoot $seleniumTarget
Write-ToolMetadata -Kind "composer" -Version $composerVersion -ModuleRoot $composerTarget -EntrypointRelativePath "composer.phar" -EntrypointSha256 $composerSha256
Write-ToolMetadata -Kind "node" -Version $nodeVersion -ModuleRoot $nodeTarget -EntrypointRelativePath "node.exe" -EntrypointSha256 $dependencies.node.normalizedEntrypointSha256
Write-ToolMetadata -Kind "python" -Version $pythonVersion -ModuleRoot $pythonTarget -EntrypointRelativePath "python.exe" -EntrypointSha256 $pythonEntrypointSha256
Write-ToolMetadata -Kind "editor" -Version $editorVersion -ModuleRoot $editorTarget -EntrypointRelativePath "notepad++.exe" -EntrypointSha256 $editorEntrypointSha256

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
        [ordered]@{ name = "Apache"; version = $apacheVersion; source = $catalogByKind.apache.sourceUrl; archiveSha256 = $dependencies.apache.archiveSha256; entrypointSha256 = $catalogByKind.apache.entrypointSha256 }
        [ordered]@{ name = "PHP"; version = $phpVersion; source = $catalogByKind.php.sourceUrl; archiveSha256 = $dependencies.php.archiveSha256; entrypointSha256 = $catalogByKind.php.entrypointSha256 }
        [ordered]@{ name = "MariaDB"; version = $mariaDbVersion; source = $catalogByKind.mariaDb.sourceUrl; archiveSha256 = $dependencies.mariadb.archiveSha256; entrypointSha256 = $catalogByKind.mariaDb.entrypointSha256 }
        [ordered]@{ name = "Selenium Server"; version = $seleniumVersion; source = $catalogByKind.selenium.sourceUrl; archiveSha256 = $dependencies.selenium.archiveSha256; entrypointSha256 = $catalogByKind.selenium.entrypointSha256 }
        [ordered]@{ name = "Microsoft OpenJDK"; version = $javaVersion; source = $dependencies.openjdk.sources[0]; archiveSha256 = $dependencies.openjdk.archiveSha256; mode = "reduced-with-profile-extension-build-tools" }
        [ordered]@{ name = "Composer"; version = $composerVersion; source = $dependencies.composer.sources[0]; sha256 = $composerSha256 }
        [ordered]@{ name = "Node.js"; version = $nodeVersion; source = $dependencies.node.sources[0]; archiveSha256 = $dependencies.node.archiveSha256; entrypointSha256 = $dependencies.node.normalizedEntrypointSha256 }
        [ordered]@{ name = "Python"; version = $pythonVersion; source = $dependencies.python.sources[0]; archiveSha256 = $dependencies.python.archiveSha256; entrypointSha256 = $pythonEntrypointSha256; pip = (& $pythonExecutable -I -m ensurepip --version) }
        [ordered]@{ name = "Notepad++"; version = $editorVersion; source = $dependencies.notepadpp.sources[0]; archiveSha256 = $dependencies.notepadpp.archiveSha256; entrypointSha256 = $editorEntrypointSha256; mode = "portable-minimal" }
        [ordered]@{ name = "phpMyAdmin"; version = $phpMyAdminVersion; source = $dependencies.phpmyadmin.sources[0]; archiveSha256 = $dependencies.phpmyadmin.archiveSha256; composerLockSha256 = $phpMyAdminComposerLockSha256 }
        [ordered]@{ name = "Microsoft Visual C++ Redistributable"; version = $vcRedistVersion; source = $dependencies.vcredist.sources[0]; archiveSha256 = $dependencies.vcredist.archiveSha256; mode = "app-local-extracted" }
    )
}
$releaseDocumentsPath = Join-Path $resolvedOutput "docs"
New-Item -ItemType Directory -Path $releaseDocumentsPath -Force | Out-Null
$bundleManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $releaseDocumentsPath "bundle-manifest.json") -Encoding utf8

Write-Host "Offline dependencies bundled into: $resolvedOutput"
}
finally {
    if (Test-Path -LiteralPath $dependencyExtraction) {
        $resolvedExtraction = [System.IO.Path]::GetFullPath($dependencyExtraction)
        $expectedPrefix = $temporaryRoot + [System.IO.Path]::DirectorySeparatorChar + "PortableDeveloperBundle-Dependencies-"
        if (-not $resolvedExtraction.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove an unexpected dependency staging path: $resolvedExtraction"
        }

        Remove-Item -LiteralPath $dependencyExtraction -Recurse -Force
    }
}
