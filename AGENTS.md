# Rules for contributors and agents

## Project priority

Portable Developer must remain genuinely portable. Every change should protect isolation from the host Windows installation, code clarity, and useful diagnostics.

## Mandatory rules

1. Do not install Windows services, drivers, or system dependencies.
2. Do not modify the system `PATH`, registry, file associations, hosts file, or firewall without an explicit architectural decision from the project owner.
3. Persist paths relative to the application root; never hard-code a drive letter or user profile.
4. Store database data, server configuration, caches, temporary files, and logs under the instance or application root.
5. Every external process must have a clear owner, working directory, captured output, health check, and safe termination path.
6. Do not execute downloaded binaries without verifying the expected SHA-256 and recording source and version.
7. Do not commit secrets, passwords, API keys, database data, profiles, downloaded binaries, or user state.
8. Record architecture changes in `docs/DECISIONS.md`, user-visible changes in `CHANGELOG.md`, and significant work in `docs/WORKLOG.md`.
9. Run relevant formatting, build, and tests before handoff. State precisely why if verification is impossible.
10. The runtime downloader may run only after an explicit user action, use only the bundled versioned catalog and allowlisted HTTPS sources, require pinned SHA-256, stage inside the portable root, and never install a system runtime or accept an arbitrary URL.

## Repository work

- Read documentation relevant to the change first.
- Keep modules small and independent; UI code must use service abstractions rather than directly controlling processes.
- Logs must be useful and must not contain secrets.
- Every setting needs a default, validation, and Czech/English UI text.
- Prefer small, focused commits following `docs/COMMITS.md`.

The repository uses .NET SDK 10.0.400 pinned in `global.json`. The target application is published self-contained and must not require a system .NET runtime.
