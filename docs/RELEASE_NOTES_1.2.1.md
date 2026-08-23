# Portable Developer 1.2.1

Portable Developer 1.2.1 is a focused reliability hotfix for the command runner introduced in 1.2.0.

## Fixed

- Restored one-shot commands that do not redirect standard input.
- Python preparation, Composer operations, and MariaDB initialization no longer fail while configuring process encoding.
- Interactive terminal commands retain UTF-8 input and output, including Czech and other Unicode text.
- Added regression coverage for both interactive input and commands without standard input.

## Verified

- Installed all seven optional modules in a clean portable copy.
- Installed the Python `translate` package and Composer `php-webdriver/webdriver` package with their dependencies.
- Started Apache, PHP, and MariaDB and verified the default project over HTTP.
- Confirmed responsive centralized progress handling and stable idle behavior during the end-to-end test.

## Download and verification

Download `PortableDeveloper-win-x64-1.2.1.zip` and verify it with `PortableDeveloper-win-x64-1.2.1.zip.sha256`. Extract the complete ZIP to a writable folder or external drive and run `PortableDeveloper.exe`.

This build is self-contained but **not digitally signed yet**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer. Verify the checksum and review the public source and GitHub Actions release workflow.
