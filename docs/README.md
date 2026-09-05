# Documentation

Portable Developer keeps its canonical project documentation in English. The application itself includes separate English and Czech user guides that work offline.

## Product and architecture

- [Architecture](ARCHITECTURE.md) — layers, services, projects, UI operation model, database, and Selenium design.
- [Portability contract](PORTABILITY.md) — what remains inside the portable root and which host-system changes are forbidden.
- [Modules](MODULES.md) — optional capability boundaries and runtime requirements.
- [Runtime layout](RUNTIMES.md) — managed runtime locations and ownership.
- [Roadmap](ROADMAP.md) — completed foundations and real planned work.
- [Central project management plan](PROJECT_MANAGEMENT_PLAN.md) — accepted model, migration, stages, and verification checklist for general projects.

## Security and supply chain

- [Security model](SECURITY_MODEL.md) — trust boundaries, network exposure, sensitive data, and explicit non-goals.
- [Code signing policy](CODE_SIGNING_POLICY.md) — roles, approval, provenance, eligible files, privacy, and removal.
- [SignPath integration](SIGNPATH_INTEGRATION.md) — intended trusted-build and artifact-signing flow after approval.
- [Package catalog](PACKAGE_CATALOG.md) — allowlisted upstream artifacts, hashes, extraction, and verification.
- [Windows reputation](WINDOWS_REPUTATION.md) — unsigned-build behavior and false-positive handling.
- [Privacy policy](../PRIVACY.md), [security policy](../SECURITY.md), and [third-party notices](../THIRD-PARTY-NOTICES.md).

## Development and releases

- [Development](DEVELOPMENT.md) — pinned SDK, locked restore, verification, and publishing.
- [Architecture decisions](DECISIONS.md) — durable technical decisions and their rationale.
- [Changelog](../CHANGELOG.md) — user-visible changes by version.
- Version-specific release notes are stored as `RELEASE_NOTES_<version>.md` and published with each GitHub Release.

Contributions use the repository [contribution guide](../CONTRIBUTING.md), [governance](../GOVERNANCE.md), and [Code of Conduct](../CODE_OF_CONDUCT.md).
