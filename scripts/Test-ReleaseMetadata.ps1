[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath,

    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$resolvedExecutable = [System.IO.Path]::GetFullPath($ExecutablePath)
if (-not (Test-Path -LiteralPath $resolvedExecutable -PathType Leaf)) {
    throw "Release executable was not found: $resolvedExecutable"
}

$parsedVersion = [Version]::Parse($Version)
$expectedFileVersion = if ($parsedVersion.Revision -ge 0) {
    $parsedVersion.ToString(4)
}
else {
    "$($parsedVersion.ToString(3)).0"
}

$metadata = (Get-Item -LiteralPath $resolvedExecutable).VersionInfo
$expected = [ordered]@{
    ProductName = "Portable Developer"
    FileDescription = "Portable Developer"
    CompanyName = "Portable Developer contributors"
    ProductVersion = $Version
    FileVersion = $expectedFileVersion
}

foreach ($item in $expected.GetEnumerator()) {
    $actual = [string]$metadata.($item.Key)
    if ($actual -ne $item.Value) {
        throw "Release metadata mismatch for $($item.Key). Expected '$($item.Value)', got '$actual'."
    }
}

Write-Host "Release metadata verified: Portable Developer $Version"
