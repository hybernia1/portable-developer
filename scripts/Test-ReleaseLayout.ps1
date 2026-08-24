[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Container)) {
    throw "Published application directory was not found: $resolvedOutput"
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
