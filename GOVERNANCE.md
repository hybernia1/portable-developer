# Governance

Portable Developer is an open-source project maintained in the public repository <https://github.com/hybernia1/portable-developer>.

## Current roles

- maintainer, committer, and reviewer: [@hybernia1](https://github.com/hybernia1);
- release and code-signing approver: [@hybernia1](https://github.com/hybernia1);
- external contributors: anyone contributing through a pull request under GPL-3.0-or-later.

The project currently has one maintainer, so these roles are held by the same person. New maintainers must demonstrate sustained, constructive participation before receiving write or signing approval access. Role changes are recorded publicly in this file.

## Decisions and reviews

Architecture decisions are recorded in `docs/DECISIONS.md`; user-visible changes are recorded in `CHANGELOG.md`. Contributions from people without commit access require a pull request, passing CI, and maintainer review. Security-sensitive reports use private GitHub Security Advisories.

Build scripts, GitHub workflows, dependency catalogs, release policy, and code-signing configuration are signing-sensitive. Review of these files must verify source provenance, hashes, permissions, and whether a change could cause a binary not represented by the public source to be signed.

## Releases and signing

Every release is based on a public version tag and an automated GitHub Actions build. Each future signing request requires a separate manual approval by an approver using multi-factor authentication. The project certificate must sign only project-owned binaries built from this repository and must never re-sign third-party components.

See the complete [Code signing policy](docs/CODE_SIGNING_POLICY.md).
