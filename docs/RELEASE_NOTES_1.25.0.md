# Portable Developer 1.25.0

This unsigned feature release changes the online Windows package from an extracted ZIP layout to one self-contained executable.

## Added

- The public Windows download is one versioned EXE. Place it in its own writable folder or external drive and run it directly.
- On first launch, the application creates its portable folder structure and materializes the bundled catalogs, resources, notices, and app-local Visual C++ runtime beside itself.
- A versioned state marker records successful seed initialization and makes an interrupted first launch safely repeatable.

## Security

- The embedded seed has a bounded entry count and extracted size. Every path, length, and SHA-256 digest is validated before an app-owned file is installed.
- Path traversal, unexpected archive entries, duplicate paths, reparse points, and malformed or oversized seed content are rejected.
- Existing projects, profiles, downloads, databases, settings, and other user data are never part of the seed and are not replaced or removed during startup.
- The Microsoft Visual C++ runtime remains recovered at build time only from the pinned redistributable after SHA-256, version, and Microsoft Authenticode validation.

## Verification and upgrade

Download `PortableDeveloper-win-x64-1.25.0.exe` and `PortableDeveloper-win-x64-1.25.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, place it in a writable folder, and run it. For an upgrade, stop all portable services, close the previous application, back up important portable data, remove the old application executable, and place the new executable in the same root. The first start refreshes only application-owned seed files and retains user data.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
