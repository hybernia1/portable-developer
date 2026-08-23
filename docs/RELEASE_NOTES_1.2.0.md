# Portable Developer 1.2.0

Portable Developer 1.2.0 adds practical offline integration guides and turns the restricted project terminal into a responsive interactive environment while preserving the application's portable and shell-free security boundaries.

## Built-in offline guides

- A new always-available Guides page provides separate Czech and English documentation embedded directly in the application.
- Numbered, tagged chapters cover setup, current local endpoints, Selenium with Python or PHP, immutable master profiles, account-independent cookie vaults, persistent downloads, and MariaDB.
- Examples use the currently configured application ports and provide selectable, copyable code without loading a web engine or remote documentation.
- The quick start explicitly identifies the optional `selenium` and `php-webdriver/webdriver` packages required by user projects.

## Responsive interactive terminal

- Bundled Python, PHP, and Composer processes now stream output while they run instead of waiting until process exit.
- Python `input()` and other line-oriented programs can receive text directly from the application terminal; Ctrl+C stops the owned process tree.
- Input, output, and error streams use UTF-8 without a byte-order mark, preserving Czech and other Unicode text from the first line.
- Python runs in UTF-8 unbuffered mode so prompts without a trailing newline and incremental progress appear immediately.
- Output updates are batched onto the WPF dispatcher and both pending and visible terminal text remain bounded, preventing chatty tools from monopolizing the interface or growing memory indefinitely.

## More useful project commands

- The terminal adds `cat`, `touch`, `cp`, `mv`, `rm`, `rmdir`, and `echo` alongside the existing navigation, runtime, and service commands.
- File operations remain confined to the active project, reject reparse-point escapes, do not overwrite destinations, and permit only single-file or empty-directory deletion.
- The terminal still does not expose `cmd.exe`, PowerShell, arbitrary executables, host `PATH` inheritance, pipes, redirects, shell chaining, or recursive deletion.

## Download and verification

Download `PortableDeveloper-win-x64-1.2.0.zip` and verify it with `PortableDeveloper-win-x64-1.2.0.zip.sha256`. Extract the complete ZIP to a writable folder or external drive and run `PortableDeveloper.exe`.

This build is self-contained but **not digitally signed yet**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer. Verify the checksum and review the public source and GitHub Actions release workflow.
