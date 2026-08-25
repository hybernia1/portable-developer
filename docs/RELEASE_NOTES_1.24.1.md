# Portable Developer 1.24.1

This unsigned patch release fixes process cleanup for interactive Node.js and Vite development servers.

## Fixed

- Node.js and Vite sessions started from the portable terminal are now assigned to an owned Windows Job Object.
- `Ctrl+C`, normal application shutdown, and a forced application exit terminate the npm/Vite process tree and release its listening port.

## Verification and upgrade

Download `PortableDeveloper-win-x64-1.24.1.zip` and its adjacent `.sha256` file, verify the archive before extraction, then extract the complete ZIP to a writable folder or external drive. Existing portable data remains in place when upgrading over an existing installation; back it up first if it matters to you.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
