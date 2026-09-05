# Code-signing policy

## Current status

Public code signing is not active yet. Version 1.28.0 is a complete but unsigned binary release, and its executable, manifest, and notes identify that state. Windows Smart App Control or SmartScreen may block it. The project does not advise users to disable Windows security. Future releases are intended to be signed after SignPath Foundation approval.

**Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).** This describes the intended future signing arrangement; releases explicitly marked unsigned do not carry that signature.

## Responsibilities and approval

The project is currently maintained by [@hybernia1](https://github.com/hybernia1), who acts as author, committer, reviewer, release author, and signing approver. Signing-sensitive paths are protected by [CODEOWNERS](../.github/CODEOWNERS). External contributions must pass public CI and review. Changes to release workflows, build scripts, catalogs, dependencies, and this policy require an explicit provenance and impact review.

Repository and signing accounts must use multi-factor authentication. Every signing request requires manual approval; creating a tag alone must never authorize signing.

## Signing scope

The project certificate may sign only binaries built from this repository, principally `PortableDeveloper.exe`. It must never re-sign Apache, PHP, MariaDB, Selenium, Java, Python, Notepad++, phpMyAdmin, browsers, WebDrivers, or other third-party components. They retain their publisher signature, hash, and license status.

Every signed artifact must trace to a public commit or tag, the SDK pinned in `global.json`, public CI, and source/version/license/SHA-256 records for release inputs. The tag workflow builds a self-contained executable and checksum from public source. Unsigned status must never be obscured or presented as certificate trust.

The only project-owned PE file eligible for the Portable Developer signature is `PortableDeveloper.exe`. App-local Microsoft runtime DLLs and all downloaded Apache, PHP, MariaDB, Java, browser, driver, editor, Python, Composer, and phpMyAdmin files are third-party components and must never be signed with the project certificate. Artifact configuration must enforce the application name, company, original filename, product version, and file version before signing.

After approval, signing requests will originate only from a GitHub-hosted release workflow artifact, use SignPath origin verification, and require a separate manual approval. The signed application must be returned to the same workflow, verified, and only then published as the public executable. See the [SignPath integration plan](SIGNPATH_INTEGRATION.md).

## Privacy and removal

The application sends nothing to maintainers without an explicit user action. See [Privacy](../PRIVACY.md). It installs no service and modifies no system `PATH`, registry, file association, hosts file, or firewall entry. Removal is stopping services, closing the application, and deleting its folder after backing up wanted portable data.
