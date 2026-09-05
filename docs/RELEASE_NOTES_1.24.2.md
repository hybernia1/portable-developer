# Portable Developer 1.24.2

This unsigned maintenance release removes WiX from native runtime packaging and refreshes the security and release automation.

## Changed

- App-local Microsoft Visual C++ runtime libraries are now recovered from the pinned redistributable without WiX or executing the installer.
- Release packaging uses the explicit Windows `System32\expand.exe` for standard embedded CAB files and keeps all staging below the portable repository root.
- GitHub release, provenance, dependency-review, and CodeQL actions were updated to their current pinned versions.
- Future Dependabot updates for CodeQL initialization and analysis are grouped so both workflow steps remain on the same version.

## Fixed

- Native runtime extraction is compatible with Windows PowerShell 5.1 used by the GitHub release runner, and regular CI now validates that compatibility before a release tag is created.

## Security

- Native runtime packaging rejects unexpected container layouts, excessive nesting, file counts, sizes, reparse points, process timeouts, and path escapes.
- The redistributable and every selected x64 runtime DLL must match their pinned SHA-256 values, expected version, and Microsoft Authenticode signer.

## Verification and upgrade

Download `PortableDeveloper-win-x64-1.24.2.zip` and its adjacent `.sha256` file, verify the archive before extraction, then extract the complete ZIP to a writable folder or external drive. Existing portable data remains in place when upgrading over an existing installation; back it up first if it matters to you.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
