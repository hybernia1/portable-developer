# Portable Developer 1.27.0

This unsigned feature release makes the application quieter and more direct, and replaces the basic project file browser with an Explorer-like file manager.

## Cleaner navigation and workspaces

- Projects is now the initial page. The incomplete environment overview and its duplicated service actions were removed.
- Project details no longer repeat identity and activation controls already available in the global project selector.
- Page taglines, the repeated terminal project banner, and redundant file-manager and storage explanations were removed.
- Settings now uses the same horizontal-tab layout as the rest of the application, with focused General, Storage, and About sections.
- The generic Tools page was removed. PHP configuration remains on the PHP page, and files use the editor preference selected in Settings.

## Explorer-like project files

- The file manager now uses type-aware icons, row selection, double-click or Enter to open, and F2 or a second name click for inline rename.
- Ctrl/Shift multi-selection and `Ctrl+A` work with copy, cut, paste, deletion, and drag export.
- `Ctrl+C`, `Ctrl+X`, and `Ctrl+V` provide a project-scoped clipboard without silently importing arbitrary host clipboard paths.
- Files and folders can be dragged out to Windows, while an explicit drag from Windows copies them into the current project directory or a targeted folder.
- Internal drag-and-drop moves items. Dropping back into the same directory or onto the source itself is a no-op.
- Name collisions offer Overwrite, Rename copy, or Skip, with an apply-to-all option for the remaining queue. Folder overwrite merges contents instead of deleting unrelated destination files.
- Application-styled context menus expose only actions that make sense for the exact click target.

## Safety and upgrade

All file operations remain confined to the active project root, reject reparse-point escapes and recursive self-copies, and run recursive work away from the UI thread. The active-project switch clears the internal file clipboard.

The release passed locked restore, formatting, Release build, 276 automated tests, dependency-catalog validation, release metadata/layout checks, and live portable UI verification on a separate `E:` installation.

Download `PortableDeveloper-win-x64-1.27.0.exe` and `PortableDeveloper-win-x64-1.27.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, stop all portable services, close the previous application, back up important portable data, replace the old executable, and start the new one. The first start refreshes only application-owned seed files and retains projects, profiles, downloads, databases, settings, and other user data.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
