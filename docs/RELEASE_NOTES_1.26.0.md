# Portable Developer 1.26.0

This unsigned feature release turns projects into general portable workspaces instead of treating every project as an Apache website. It also makes Apache useful without PHP and improves several first-use workflows discovered during clean-drive testing.

## Projects

- A global active-project selector now supplies one shared project context to Files, Terminal, Composer, npm, and Selenium downloads.
- The dedicated Projects page supports Empty, Web, Python, Browser Automation, and Node.js starter templates without downloading or executing dependencies.
- Projects can be inspected, activated, renamed, opened, and unregistered without deleting their files.
- Existing real directories already below the managed projects root can be registered without rewriting their contents.
- Bounded, read-only capability detection explains which tools fit a project and which shared runtimes are missing.
- Valid legacy web projects migrate non-destructively into a versioned general project catalog. The legacy catalog remains untouched for rollback during the compatibility period.

## Web development

- Apache can start independently and serve static projects when PHP is not installed. When verified PHP is available, Apache continues to own its FastCGI worker automatically.
- Web root, Apache enablement, and `.htaccess` are optional project settings managed together from Projects.
- Creating a web project or enabling web support later creates a static `index.html` only when it is absent. Existing project files are never overwritten and PHP is not required for the starter page.
- Changes are persisted immediately, while a running Apache instance adopts them only after the explicit apply-and-restart action.
- Static-only Apache configuration denies PHP source files and does not expose phpMyAdmin.

## Usability and safety

- Settings allow text and source files to use verified portable Notepad++ or their Windows default application.
- Selenium explains the possible Windows Java firewall prompt once while remaining bound to loopback and never changing firewall rules.
- The portable terminal accepts markup and shell-operator characters as literal arguments and adds explicit project-rooted overwrite and append operations without exposing a host shell.
- The Projects page separates browsing, creation, and registration into horizontal tabs with one focused project detail panel.

## Verification and upgrade

The release passed locked restore, formatting, Release build, 272 automated tests, dependency-catalog validation, release metadata/layout checks, and live portable UI testing on a separate `E:` installation.

Download `PortableDeveloper-win-x64-1.26.0.exe` and `PortableDeveloper-win-x64-1.26.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, stop all portable services, close the previous application, back up important portable data, replace the old executable, and start the new one. The first start refreshes only application-owned seed files and retains projects, profiles, downloads, databases, settings, and other user data.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
