# Portable Developer 1.24.0

This unsigned release adds an optional portable Node.js 24.19.0 runtime and npm package management for Web Projects.

## Highlights

- Install Node.js with the same pinned-source, SHA-256 verification, safe extraction, repair, and removal workflow used by the other optional runtimes.
- Manage direct npm dependencies from the Node.js page: inspect direct and transitive packages, install a named package, refresh the list, or remove a direct package.
- Keep npm cache and configuration inside the portable application root. Package lifecycle commands run from the active Web Project and disable npm lifecycle scripts.
- Package-management pages now scroll correctly on smaller application windows.

## Verification and upgrade

Download `PortableDeveloper-win-x64-1.24.0.zip` and its adjacent `.sha256` file, verify the archive before extraction, then extract the complete ZIP to a writable folder or external drive. Existing portable data remains in place when upgrading over an existing installation; back it up first if it matters to you.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
