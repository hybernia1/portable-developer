[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$PublishRoot = (Join-Path $PSScriptRoot "..\artifacts\publish"),

    [ValidateRange(1, 20)]
    [int]$Keep = 2
)

$ErrorActionPreference = "Stop"

$expectedRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\artifacts\publish"))
$resolvedRoot = [System.IO.Path]::GetFullPath($PublishRoot)
if (-not [string]::Equals($resolvedRoot, $expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Release cleanup is restricted to the repository publish directory: $expectedRoot"
}

if (-not (Test-Path -LiteralPath $resolvedRoot -PathType Container)) {
    Write-Host "Release directory does not exist: $resolvedRoot"
    return
}

$releaseDirectories = @(Get-ChildItem -LiteralPath $resolvedRoot -Directory -Force |
    Sort-Object LastWriteTime -Descending)
$keptDirectories = @($releaseDirectories | Select-Object -First $Keep)
$keptPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($directory in $keptDirectories) {
    $null = $keptPaths.Add([System.IO.Path]::GetFullPath($directory.FullName))
}

$runningExecutables = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.ExecutablePath) } |
    Select-Object -ExpandProperty ExecutablePath)
foreach ($directory in $releaseDirectories) {
    $directoryPath = [System.IO.Path]::GetFullPath($directory.FullName)
    $directoryPrefix = $directoryPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    if ($runningExecutables | Where-Object {
            [System.IO.Path]::GetFullPath($_).StartsWith($directoryPrefix, [StringComparison]::OrdinalIgnoreCase)
        }) {
        $null = $keptPaths.Add($directoryPath)
    }
}

$candidates = @(Get-ChildItem -LiteralPath $resolvedRoot -Force | Where-Object {
        -not ($_.PSIsContainer -and $keptPaths.Contains([System.IO.Path]::GetFullPath($_.FullName)))
    })

foreach ($candidate in $candidates) {
    $candidatePath = [System.IO.Path]::GetFullPath($candidate.FullName)
    if ([System.IO.Path]::GetDirectoryName($candidatePath) -ne $resolvedRoot -or
        -not $candidate.Name.StartsWith("PortableDeveloper", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove an unexpected publish entry: $candidatePath"
    }

    if (-not $PSCmdlet.ShouldProcess($candidatePath, "Remove old release artifact")) {
        continue
    }

    $extendedPath = "\\?\$candidatePath"
    if ($candidate.PSIsContainer) {
        [System.IO.Directory]::Delete($extendedPath, $true)
    }
    else {
        [System.IO.File]::Delete($extendedPath)
    }

    Write-Host "Removed old release artifact: $candidatePath"
}

Write-Host "Kept release directories:"
Get-ChildItem -LiteralPath $resolvedRoot -Directory -Force |
    Sort-Object LastWriteTime -Descending |
    ForEach-Object { Write-Host "- $($_.Name)" }
