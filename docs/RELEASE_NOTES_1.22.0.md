# Portable Developer 1.22.0

Portable Developer 1.22.0 improves visual clarity during first use and makes the portable release layout cleaner and easier to inspect.

## Changed

- The release root now contains one `PortableDeveloper.exe` and clearly organized folders for catalogs, resources, runtime support, and documentation.
- Module cards and installed-technology navigation use recognizable technology marks, while application actions retain neutral system icons.
- Sidebar group labels are passive headings, avoiding controls that look clickable when they are not.
- Runtime downloads and Composer/Python package operations now share clear operation status, detail, and progress feedback.
- The project file manager recognizes more file types, including HTML/XML, executables, text and Markdown, JSON/YAML, images, archives, databases, configuration, source files, Python, Java archives, documents, and spreadsheets.
- The Selenium module badge now shows its version only, matching the other single-product modules.

## Verification

- The release root is validated to reject unexpected top-level files.
- The application version and executable metadata are verified during packaging.
- The normal formatting, build, test, and catalog-validation checks were run before publication.

## Download and verification

Download `PortableDeveloper-win-x64-1.22.0.zip` and verify it with the adjacent `PortableDeveloper-win-x64-1.22.0.zip.sha256` file. Extract the complete ZIP to a writable folder or external drive and run `PortableDeveloper.exe`.

This self-contained build is currently **not digitally signed**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer; verify the checksum and review the public source and release workflow instead.

## Code signing policy

This release remains unsigned. Future signing follows the public [Code signing policy](https://github.com/hybernia1/portable-developer/blob/main/docs/CODE_SIGNING_POLICY.md).
