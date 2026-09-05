function Test-PdCabinetHeader {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        if ($stream.Length -lt 36) {
            return $false
        }

        $header = New-Object byte[] 36
        if ($stream.Read($header, 0, $header.Length) -ne $header.Length) {
            return $false
        }

        $cabinetSize = [BitConverter]::ToUInt32($header, 8)
        $filesOffset = [BitConverter]::ToUInt32($header, 16)
        $folderCount = [BitConverter]::ToUInt16($header, 26)
        $fileCount = [BitConverter]::ToUInt16($header, 28)
        $trailingBytes = $stream.Length - $cabinetSize
        if ($header[0] -eq 0x4d -and
            $header[1] -eq 0x53 -and
            $header[2] -eq 0x43 -and
            $header[3] -eq 0x46) {
            Write-Verbose "CAB header ${Path}: stream=$($stream.Length), declared=$cabinetSize, filesOffset=$filesOffset, version=$($header[25]).$($header[24]), folders=$folderCount, files=$fileCount"
        }

        return $header[0] -eq 0x4d -and
            $header[1] -eq 0x53 -and
            $header[2] -eq 0x43 -and
            $header[3] -eq 0x46 -and
            $header[24] -eq 3 -and
            $header[25] -eq 1 -and
            $cabinetSize -le $stream.Length -and
            $trailingBytes -ge 0 -and
            $trailingBytes -le 1MB -and
            $filesOffset -ge 36 -and
            $filesOffset -lt $cabinetSize -and
            $folderCount -gt 0 -and
            $folderCount -le 64 -and
            $fileCount -gt 0 -and
            $fileCount -le 4096
    }
    finally {
        $stream.Dispose()
    }
}

function Find-PdEmbeddedCabinets {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $maximumInputSizeBytes = 512MB
    $maximumCabinetSizeBytes = 256MB
    $maximumCabinetCount = 32
    $file = Get-Item -LiteralPath $Path
    if ($file.Length -lt 36 -or $file.Length -gt $maximumInputSizeBytes) {
        throw "VC++ Redistributable has an unsupported size: $($file.Length) bytes."
    }

    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $position = 0
    $results = [System.Collections.Generic.List[object]]::new()
    while ($position -le $bytes.Length - 36) {
        $offset = [Array]::IndexOf[byte]($bytes, [byte]0x4d, $position)
        if ($offset -lt 0 -or $offset -gt $bytes.Length - 36) {
            break
        }

        if ($bytes[$offset + 1] -eq 0x53 -and
            $bytes[$offset + 2] -eq 0x43 -and
            $bytes[$offset + 3] -eq 0x46) {
            $cabinetSize = [BitConverter]::ToUInt32($bytes, $offset + 8)
            $filesOffset = [BitConverter]::ToUInt32($bytes, $offset + 16)
            $folderCount = [BitConverter]::ToUInt16($bytes, $offset + 26)
            $fileCount = [BitConverter]::ToUInt16($bytes, $offset + 28)
            $endOffset = [int64]$offset + $cabinetSize
            if ($bytes[$offset + 24] -eq 3 -and
                $bytes[$offset + 25] -eq 1 -and
                $cabinetSize -ge 36 -and
                $cabinetSize -le $maximumCabinetSizeBytes -and
                $filesOffset -ge 36 -and
                $filesOffset -lt $cabinetSize -and
                $folderCount -gt 0 -and
                $folderCount -le 64 -and
                $fileCount -gt 0 -and
                $fileCount -le 4096 -and
                $endOffset -le $bytes.LongLength) {
                $results.Add([pscustomobject]@{
                    Offset = [int64]$offset
                    Size = [uint32]$cabinetSize
                })
                if ($results.Count -gt $maximumCabinetCount) {
                    throw "VC++ Redistributable contains more than $maximumCabinetCount valid CAB segments."
                }
            }
        }

        $position = $offset + 1
    }

    if ($results.Count -eq 0) {
        throw "VC++ Redistributable does not contain a supported CAB segment."
    }

    return $results.ToArray()
}

function Copy-PdFileSegment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,

        [Parameter(Mandatory = $true)]
        [int64]$Offset,

        [Parameter(Mandatory = $true)]
        [uint32]$Length,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    $input = [System.IO.File]::OpenRead($SourcePath)
    try {
        if ($Offset -lt 0 -or $Length -lt 36 -or $Offset + $Length -gt $input.Length) {
            throw "Refusing to copy an invalid CAB segment from $SourcePath."
        }

        $input.Position = $Offset
        $output = [System.IO.File]::Create($DestinationPath)
        try {
            $buffer = New-Object byte[] (1MB)
            $remaining = [int64]$Length
            while ($remaining -gt 0) {
                $read = $input.Read($buffer, 0, [Math]::Min($buffer.Length, $remaining))
                if ($read -le 0) {
                    throw "Unexpected end of file while copying a CAB segment from $SourcePath."
                }

                $output.Write($buffer, 0, $read)
                $remaining -= $read
            }
        }
        finally {
            $output.Dispose()
        }
    }
    finally {
        $input.Dispose()
    }
}

function Expand-PdCabinet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CabinetPath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    if (Test-Path -LiteralPath $DestinationPath) {
        throw "CAB extraction destination already exists: $DestinationPath"
    }

    if (-not (Test-Path -LiteralPath $WorkingDirectory -PathType Container)) {
        throw "CAB extraction working directory was not found: $WorkingDirectory"
    }

    New-Item -ItemType Directory -Path $DestinationPath | Out-Null
    $expandPath = Join-Path $env:SystemRoot "System32\expand.exe"
    if (-not (Test-Path -LiteralPath $expandPath -PathType Leaf)) {
        throw "Windows expand.exe was not found: $expandPath"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $expandPath
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add($CabinetPath)
    $startInfo.ArgumentList.Add("-F:*")
    $startInfo.ArgumentList.Add($DestinationPath)

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $processStarted = $false
    try {
        if (-not $process.Start()) {
            throw "Windows expand.exe could not be started."
        }
        $processStarted = $true

        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "Windows expand.exe timed out while extracting $CabinetPath."
        }

        $outputText = $standardOutput.GetAwaiter().GetResult()
        $errorText = $standardError.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) {
            $diagnostic = ($outputText + [Environment]::NewLine + $errorText).Trim()
            if ($diagnostic.Length -gt 2000) {
                $diagnostic = $diagnostic.Substring($diagnostic.Length - 2000)
            }

            throw "Windows expand.exe failed for $CabinetPath (exit code $($process.ExitCode)): $diagnostic"
        }
    }
    finally {
        if ($processStarted -and -not $process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        $process.Dispose()
    }

    $destinationPrefix = [System.IO.Path]::GetFullPath($DestinationPath).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    foreach ($item in Get-ChildItem -LiteralPath $DestinationPath -Recurse -Force) {
        $resolvedItem = [System.IO.Path]::GetFullPath($item.FullName)
        if (-not $resolvedItem.StartsWith($destinationPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "CAB extraction escaped its staging directory: $resolvedItem"
        }

        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "CAB extraction produced a reparse point: $resolvedItem"
        }
    }
}

function Expand-PdVerifiedVcRuntime {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallerPath,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedInstallerSha256,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedSignerSubjectContains,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion,

        [Parameter(Mandatory = $true)]
        [object]$RuntimeFiles,

        [Parameter(Mandatory = $true)]
        [string]$StagingRoot,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $resolvedInstaller = [System.IO.Path]::GetFullPath($InstallerPath)
    if (-not (Test-Path -LiteralPath $resolvedInstaller -PathType Leaf)) {
        throw "VC++ Redistributable was not found: $resolvedInstaller"
    }

    if ($ExpectedInstallerSha256 -notmatch '^[0-9a-fA-F]{64}$') {
        throw "Expected VC++ Redistributable SHA-256 is invalid."
    }

    if ([string]::IsNullOrWhiteSpace($ExpectedSignerSubjectContains)) {
        throw "Expected VC++ Redistributable signer identity is missing."
    }

    $actualInstallerHash = (Get-FileHash -LiteralPath $resolvedInstaller -Algorithm SHA256).Hash
    if ($actualInstallerHash -ne $ExpectedInstallerSha256) {
        throw "VC++ Redistributable SHA-256 mismatch. Expected $ExpectedInstallerSha256, got $actualInstallerHash."
    }

    $installerSignature = Get-AuthenticodeSignature -LiteralPath $resolvedInstaller
    if ($installerSignature.Status -ne "Valid" -or
        $installerSignature.SignerCertificate.Subject.IndexOf(
            $ExpectedSignerSubjectContains,
            [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "VC++ Redistributable signature is not trusted: $resolvedInstaller"
    }

    $parsedVersion = [Version]::Parse($ExpectedVersion)
    $runtimeProperties = @($RuntimeFiles.PSObject.Properties)
    if ($runtimeProperties.Count -eq 0 -or $runtimeProperties.Count -gt 32) {
        throw "VC++ runtime catalog must contain between 1 and 32 files."
    }

    foreach ($property in $runtimeProperties) {
        if ($property.Name -notmatch '^[A-Za-z0-9_.-]+\.dll$' -or
            [string]$property.Value -notmatch '^[0-9a-fA-F]{64}$') {
            throw "VC++ runtime catalog entry is invalid: $($property.Name)"
        }
    }

    $resolvedStagingRoot = [System.IO.Path]::GetFullPath($StagingRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $resolvedStagingRoot -PathType Container)) {
        throw "VC++ runtime staging root was not found: $resolvedStagingRoot"
    }

    $resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
    $stagingPrefix = $resolvedStagingRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedOutput.StartsWith($stagingPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "VC++ runtime output must remain below its staging root: $resolvedStagingRoot"
    }

    if (Test-Path -LiteralPath $resolvedOutput) {
        throw "VC++ runtime output already exists: $resolvedOutput"
    }

    $workRoot = Join-Path $resolvedStagingRoot ("vcredist-cab-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $workRoot | Out-Null
    try {
        $cabinetRoot = Join-Path $workRoot "cabinets"
        $extractionRoot = Join-Path $workRoot "extracted"
        $normalizedRoot = Join-Path $workRoot "normalized"
        New-Item -ItemType Directory -Path $cabinetRoot, $extractionRoot, $normalizedRoot | Out-Null

        $queue = [System.Collections.Generic.Queue[object]]::new()
        $segmentIndex = 0
        foreach ($segment in @(Find-PdEmbeddedCabinets -Path $resolvedInstaller)) {
            $cabinetPath = Join-Path $cabinetRoot ("embedded-{0:D2}.cab" -f $segmentIndex)
            Copy-PdFileSegment `
                -SourcePath $resolvedInstaller `
                -Offset $segment.Offset `
                -Length $segment.Size `
                -DestinationPath $cabinetPath
            if (-not (Test-PdCabinetHeader -Path $cabinetPath)) {
                throw "Extracted CAB segment has an invalid header: $cabinetPath"
            }

            $queue.Enqueue([pscustomobject]@{ Path = $cabinetPath; Depth = 0 })
            $segmentIndex++
        }
        Write-Verbose "Queued $segmentIndex embedded CAB segments from the verified redistributable."

        $seenCabinetHashes = [System.Collections.Generic.HashSet[string]]::new(
            [StringComparer]::OrdinalIgnoreCase)
        $extractedFiles = [System.Collections.Generic.List[object]]::new()
        $extractionIndex = 0
        $totalExtractedBytes = [int64]0
        while ($queue.Count -gt 0) {
            $cabinet = $queue.Dequeue()
            $cabinetHash = (Get-FileHash -LiteralPath $cabinet.Path -Algorithm SHA256).Hash
            if (-not $seenCabinetHashes.Add($cabinetHash)) {
                continue
            }

            if ($seenCabinetHashes.Count -gt 64) {
                throw "VC++ Redistributable contains too many nested CAB files."
            }

            $cabinetOutput = Join-Path $extractionRoot ("cabinet-{0:D2}" -f $extractionIndex)
            Expand-PdCabinet `
                -CabinetPath $cabinet.Path `
                -DestinationPath $cabinetOutput `
                -WorkingDirectory $workRoot
            $extractionIndex++

            $cabinetFiles = @(Get-ChildItem -LiteralPath $cabinetOutput -Recurse -File)
            Write-Verbose "Expanded CAB depth $($cabinet.Depth) to $($cabinetFiles.Count) files."
            foreach ($file in $cabinetFiles) {
                $extractedFiles.Add($file)
                $totalExtractedBytes += $file.Length
                if ($extractedFiles.Count -gt 4096 -or $totalExtractedBytes -gt 512MB) {
                    throw "VC++ Redistributable extraction exceeded its safety limits."
                }

                if (Test-PdCabinetHeader -Path $file.FullName) {
                    if ($cabinet.Depth -ge 2) {
                        throw "VC++ Redistributable contains CAB nesting deeper than supported."
                    }

                    $queue.Enqueue([pscustomobject]@{
                        Path = $file.FullName
                        Depth = $cabinet.Depth + 1
                    })
                }
            }
        }

        foreach ($property in $runtimeProperties) {
            $fileName = $property.Name
            $sourceName = "${fileName}_amd64"
            $expectedHash = ([string]$property.Value).ToLowerInvariant()
            $matchingSources = @(
                $extractedFiles |
                    Where-Object { $_.Name.Equals($sourceName, [StringComparison]::OrdinalIgnoreCase) })
            $validSources = @(
                $matchingSources |
                    Where-Object {
                        (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -eq $expectedHash
                    })
            if ($validSources.Count -ne 1) {
                $observedHashes = @(
                    $matchingSources |
                        ForEach-Object {
                            (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                        }) -join ", "
                throw "Expected exactly one hash-matched VC++ runtime payload for $fileName; found $($validSources.Count) valid among $($matchingSources.Count) named candidates. Observed hashes: $observedHashes"
            }

            $source = $validSources[0]
            $fileVersion = [Version]$source.VersionInfo.FileVersion
            if ($fileVersion -ne $parsedVersion) {
                throw "VC++ runtime $fileName has version $fileVersion, expected $parsedVersion."
            }

            $signature = Get-AuthenticodeSignature -LiteralPath $source.FullName
            if ($signature.Status -ne "Valid" -or
                $signature.SignerCertificate.Subject.IndexOf(
                    "O=Microsoft Corporation",
                    [StringComparison]::OrdinalIgnoreCase) -lt 0) {
                throw "VC++ runtime signature is not trusted: $($source.FullName)"
            }

            Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $normalizedRoot $fileName)
        }

        $normalizedFiles = @(Get-ChildItem -LiteralPath $normalizedRoot -File)
        if ($normalizedFiles.Count -ne $runtimeProperties.Count) {
            throw "Normalized VC++ runtime file count does not match the catalog."
        }

        Move-Item -LiteralPath $normalizedRoot -Destination $resolvedOutput
    }
    finally {
        if (Test-Path -LiteralPath $workRoot -PathType Container) {
            $resolvedWorkRoot = [System.IO.Path]::GetFullPath($workRoot)
            $expectedPrefix = $resolvedStagingRoot + [System.IO.Path]::DirectorySeparatorChar + "vcredist-cab-"
            if (-not $resolvedWorkRoot.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to remove an unexpected VC++ runtime staging path: $resolvedWorkRoot"
            }

            [System.IO.Directory]::Delete($resolvedWorkRoot, $true)
        }
    }
}
