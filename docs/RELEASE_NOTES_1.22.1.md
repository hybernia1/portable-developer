# Portable Developer 1.22.1

Portable Developer 1.22.1 makes the restricted project terminal more useful for inspecting and preparing project files while preserving its portable execution boundary.

## Added

- Project-local `find`, `grep`, and `tree` commands provide bounded file discovery without exposing a system shell.
- The non-overwriting `write` command can create a new project-local text file without replacing existing content.

## Changed

- The terminal now retains up to 250,000 visible characters and 400,000 pending output characters. It clearly reports when older output is trimmed.

## Fixed

- Direct `python -m pip` and `python -m ensurepip` invocations are blocked, preventing changes to the verified portable Python runtime from bypassing the managed package store.

## Verification

- Formatting, Release build, automated tests, and release-layout and metadata validation were run before publication.

## Download and verification

Download `PortableDeveloper-win-x64-1.22.1.zip` and verify it with the adjacent `PortableDeveloper-win-x64-1.22.1.zip.sha256` file. Extract the complete ZIP to a writable folder or external drive and run `PortableDeveloper.exe`.

This self-contained build is currently **not digitally signed**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer; verify the checksum and review the public source and release workflow instead.

## Code signing policy

This release remains unsigned. Future signing follows the public [Code signing policy](https://github.com/hybernia1/portable-developer/blob/main/docs/CODE_SIGNING_POLICY.md).
