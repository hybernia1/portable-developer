[CmdletBinding()]
param(
    [string]$CatalogPath = (Join-Path $PSScriptRoot "..\catalog\dependencies.lock.json"),
    [string]$CachePath = (Join-Path $PSScriptRoot "..\downloads\dependencies"),
    [string[]]$ComponentIds,
    [switch]$ValidateCatalogOnly,
    [switch]$Offline,
    [switch]$VerifyOnly
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
$allowedHosts = @(
    "www.apachelounge.com",
    "windows.php.net",
    "downloads.php.net",
    "downloads.mariadb.org",
    "archive.mariadb.org",
    "github.com",
    "objects.githubusercontent.com",
    "release-assets.githubusercontent.com",
    "storage.googleapis.com",
    "archive.mozilla.org",
    "aka.ms",
    "download.visualstudio.microsoft.com",
    "getcomposer.org",
    "nodejs.org",
    "api.nuget.org",
    "globalcdn.nuget.org",
    "files.phpmyadmin.net"
)

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-SourceUri {
    param([Parameter(Mandatory = $true)][string]$Source)

    $uri = [Uri]$Source
    if ($uri.Scheme -ne "https") {
        throw "Dependency source must use HTTPS: $Source"
    }

    if ($uri.Host -notin $allowedHosts) {
        throw "Dependency source host is not allowed: $($uri.Host)"
    }

    return $uri
}

function Invoke-DependencyDownload {
    param(
        [Parameter(Mandatory = $true)][Uri]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $lastError = $null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            $handler = [System.Net.Http.HttpClientHandler]::new()
            $handler.AllowAutoRedirect = $true
            $handler.MaxAutomaticRedirections = 10
            $client = [System.Net.Http.HttpClient]::new($handler)
            $client.Timeout = [TimeSpan]::FromMinutes(10)
            $response = $client.GetAsync(
                $Source,
                [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
            $null = $response.EnsureSuccessStatusCode()
            $finalUri = $response.RequestMessage.RequestUri
            if ($finalUri.Scheme -ne "https" -or $finalUri.Host -notin $allowedHosts) {
                $response.Dispose()
                $client.Dispose()
                $handler.Dispose()
                throw "Download redirected to a source host that is not allowed: $finalUri"
            }

            $input = $response.Content.ReadAsStreamAsync().GetAwaiter().GetResult()
            $output = [System.IO.File]::Create($Destination)
            try {
                $input.CopyTo($output)
            }
            finally {
                $output.Dispose()
                $input.Dispose()
                $response.Dispose()
                $client.Dispose()
                $handler.Dispose()
            }

            return
        }
        catch {
            $lastError = $_
            if (Test-Path -LiteralPath $Destination -PathType Leaf) {
                Remove-Item -LiteralPath $Destination -Force
            }

            if ($attempt -lt 3) {
                Start-Sleep -Seconds ([Math]::Pow(2, $attempt - 1))
            }
        }
    }

    throw "Download failed after three attempts: $Source. $($lastError.Exception.Message)"
}

$resolvedCatalog = [System.IO.Path]::GetFullPath($CatalogPath)
if (-not (Test-Path -LiteralPath $resolvedCatalog -PathType Leaf)) {
    throw "Dependency lock was not found: $resolvedCatalog"
}

$catalog = Get-Content -LiteralPath $resolvedCatalog -Raw | ConvertFrom-Json
if ($catalog.schemaVersion -ne 1 -or $null -eq $catalog.components) {
    throw "Unsupported dependency lock schema: $resolvedCatalog"
}

$knownComponentIds = @{}
foreach ($component in $catalog.components) {
    if ([string]::IsNullOrWhiteSpace($component.id) -or
        [string]::IsNullOrWhiteSpace($component.version) -or
        [string]::IsNullOrWhiteSpace($component.fileName) -or
        $component.archiveSha256 -notmatch "^[a-fA-F0-9]{64}$") {
        throw "Dependency lock contains an invalid component entry."
    }

    if ($knownComponentIds.ContainsKey($component.id)) {
        throw "Dependency lock contains duplicate component id: $($component.id)"
    }

    $knownComponentIds[$component.id] = $true
    if ([System.IO.Path]::GetFileName($component.fileName) -ne $component.fileName) {
        throw "Dependency file name must not contain a path: $($component.fileName)"
    }

    if ($null -eq $component.sources -or $component.sources.Count -eq 0) {
        throw "Dependency has no trusted source: $($component.id)"
    }

    foreach ($sourceText in $component.sources) {
        $null = Assert-SourceUri -Source $sourceText
    }
}

if ($ValidateCatalogOnly) {
    Write-Host "Dependency lock is valid: $resolvedCatalog"
    return
}

$componentsToProcess = @($catalog.components)
if ($null -ne $ComponentIds -and $ComponentIds.Count -gt 0) {
    $requestedIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($id in $ComponentIds) {
        if (-not $knownComponentIds.ContainsKey($id)) {
            throw "Dependency component was not found in the lock: $id"
        }

        $null = $requestedIds.Add($id)
    }

    $componentsToProcess = @($catalog.components | Where-Object { $requestedIds.Contains($_.id) })
}

$resolvedCache = [System.IO.Path]::GetFullPath($CachePath)
New-Item -ItemType Directory -Path $resolvedCache -Force | Out-Null
$cachePrefix = $resolvedCache.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

foreach ($component in $componentsToProcess) {
    $componentDirectory = Join-Path $resolvedCache (Join-Path $component.id $component.version)
    $destination = [System.IO.Path]::GetFullPath((Join-Path $componentDirectory $component.fileName))
    if (-not $destination.StartsWith($cachePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Dependency cache target escaped the cache root: $destination"
    }

    if (Test-Path -LiteralPath $destination -PathType Leaf) {
        $actualHash = Get-Sha256 -Path $destination
        if ($actualHash -eq $component.archiveSha256.ToLowerInvariant()) {
            Write-Host "[OK] $($component.displayName) $($component.version)"
            continue
        }

        if ($Offline -or $VerifyOnly) {
            throw "Cached dependency has an invalid SHA-256: $destination"
        }

        Remove-Item -LiteralPath $destination -Force
    }

    if ($Offline -or $VerifyOnly) {
        throw "Dependency is not available in the verified cache: $destination"
    }

    New-Item -ItemType Directory -Path $componentDirectory -Force | Out-Null
    $downloaded = $false
    $sourceErrors = @()
    foreach ($sourceText in $component.sources) {
        $source = Assert-SourceUri -Source $sourceText
        $partial = "$destination.$([Guid]::NewGuid().ToString('N')).part"
        try {
            Write-Host "[GET] $($component.displayName) $($component.version) <- $source"
            Invoke-DependencyDownload -Source $source -Destination $partial
            $actualHash = Get-Sha256 -Path $partial
            if ($actualHash -ne $component.archiveSha256.ToLowerInvariant()) {
                throw "SHA-256 mismatch. Expected $($component.archiveSha256), got $actualHash."
            }

            Move-Item -LiteralPath $partial -Destination $destination
            $downloaded = $true
            break
        }
        catch {
            $sourceErrors += "$source`: $($_.Exception.Message)"
        }
        finally {
            if (Test-Path -LiteralPath $partial -PathType Leaf) {
                Remove-Item -LiteralPath $partial -Force
            }
        }
    }

    if (-not $downloaded) {
        throw "No trusted source supplied $($component.displayName) $($component.version): $($sourceErrors -join ' | ')"
    }

    Write-Host "[OK] $($component.displayName) $($component.version)"
}

Write-Host "Verified dependency cache: $resolvedCache"
