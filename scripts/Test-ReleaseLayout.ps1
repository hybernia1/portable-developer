[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [switch]$SingleExecutable
)

$ErrorActionPreference = "Stop"

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Container)) {
    throw "Published application directory was not found: $resolvedOutput"
}

if ($SingleExecutable) {
    $entries = @(Get-ChildItem -LiteralPath $resolvedOutput -Force)
    if ($entries.Count -ne 1 -or
        $entries[0].PSIsContainer -or
        $entries[0].Name -ne "PortableDeveloper.exe") {
        $names = ($entries | Select-Object -ExpandProperty Name) -join ", "
        throw "Single-executable release root must contain only PortableDeveloper.exe. Found: $names"
    }

    Write-Host "Single-executable release root is clean: $resolvedOutput"
    return
}

$allowedRootEntries = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase)
foreach ($entry in @("PortableDeveloper.exe", "catalog", "docs", "drivers", "modules", "resources", "runtime", "tools")) {
    $null = $allowedRootEntries.Add($entry)
}

$unexpectedEntries = @(
    Get-ChildItem -LiteralPath $resolvedOutput -Force |
        Where-Object { -not $allowedRootEntries.Contains($_.Name) })
if ($unexpectedEntries.Count -gt 0) {
    $names = ($unexpectedEntries | Select-Object -ExpandProperty Name) -join ", "
    throw "Release root contains unexpected entries: $names"
}

foreach ($requiredDirectory in @("catalog", "docs", "resources")) {
    if (-not (Test-Path -LiteralPath (Join-Path $resolvedOutput $requiredDirectory) -PathType Container)) {
        throw "Release root is missing required directory: $requiredDirectory"
    }
}

foreach ($requiredDocument in @("LICENSE", "PRIVACY.md", "THIRD-PARTY-NOTICES.md")) {
    $documentPath = Join-Path (Join-Path $resolvedOutput "docs") $requiredDocument
    if (-not (Test-Path -LiteralPath $documentPath -PathType Leaf)) {
        throw "Release documentation is missing: $requiredDocument"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $resolvedOutput "PortableDeveloper.exe") -PathType Leaf)) {
    throw "Release executable is missing."
}

Write-Host "Release root layout is clean: $resolvedOutput"
