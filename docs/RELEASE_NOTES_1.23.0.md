# Portable Developer 1.23.0

Portable Developer 1.23.0 separates the Apache web service from the PHP runtime and makes runtime-package installation status reliable after download completion.

## Changed

- Apache and PHP are independently installable runtime packages.
- Apache is the only user-controllable web service. Its start/stop action retains ownership of the PHP FastCGI worker it requires.
- PHP now appears in the Development sidebar group. Its settings restart Apache only when Apache is already running.

## Fixed

- Completed runtime-package installations no longer remain visually stuck on a delayed downloading progress update.

## Verification

- Formatting, Release build, automated tests, release-layout and metadata validation, and a clean-drive end-to-end installation of every available module were completed before publication.

## Download and verification

Download `PortableDeveloper-win-x64-1.23.0.zip` and verify it with the adjacent `PortableDeveloper-win-x64-1.23.0.zip.sha256` file. Extract the complete ZIP to a writable folder or external drive and run `PortableDeveloper.exe`.

This self-contained build is currently **not digitally signed**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer; verify the checksum and review the public source and release workflow instead.

## Code signing policy

This release remains unsigned. Future signing follows the public [Code signing policy](https://github.com/hybernia1/portable-developer/blob/main/docs/CODE_SIGNING_POLICY.md).
