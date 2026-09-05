# SignPath integration plan

## Current status

Portable Developer has applied for the free Open Source Code Signing service. Public releases remain explicitly unsigned until SignPath Foundation approves the project and the trusted-build integration is configured. This document records the intended boundary without inventing organization, project, policy, or artifact-configuration identifiers before they are assigned.

## Eligible artifact

Only the project-owned `PortableDeveloper.exe` built from this repository is eligible for the Portable Developer certificate.

The release also contains app-local Microsoft runtime libraries, and users may download Apache, PHP, MariaDB, Selenium, OpenJDK, Chrome for Testing, ChromeDriver, Firefox, geckodriver, Python, Composer, Notepad++, and phpMyAdmin. These are independent third-party components. They retain their existing publisher signatures or unsigned state and must never receive the Portable Developer signature.

## Metadata restrictions

The SignPath artifact configuration must accept exactly one project executable and enforce values already checked by `scripts/Test-ReleaseMetadata.ps1`:

| Attribute | Required value |
|---|---|
| Artifact path | `PortableDeveloper.exe` |
| PE original filename | `PortableDeveloper.dll` |
| Product name | `Portable Developer` |
| File description | `Portable Developer` |
| Company name | `Portable Developer contributors` |
| Product version | release version parameter |
| File version | four-part release version |

The configuration must reject unexpected project PE files and must not use a wildcard signing directive over third-party DLL or EXE files.

## Intended trusted-build flow

1. A versioned public commit reaches `main` through the protected repository process.
2. An annotated `v*` tag points to a commit reachable from `main`.
3. GitHub-hosted Windows runners restore locked dependencies, verify formatting, build, test, and publish the unsigned application.
4. The workflow uploads the unsigned artifact to GitHub before submission.
5. The official SignPath GitHub integration submits that GitHub artifact with trusted origin metadata.
6. The release signing policy requires manual approval by the project approver using multi-factor authentication.
7. SignPath returns the signed project executable to the same workflow.
8. The workflow verifies Authenticode, timestamp, filename, and version metadata before packaging it with untouched third-party components.
9. The workflow publishes the final EXE, SHA-256 file, SPDX SBOM, provenance attestation, and GitHub Release.

The API token will be stored only as a GitHub Actions secret after approval. Signing identifiers will be committed explicitly to the workflow or policy configuration once assigned; they will not be accepted from untrusted pull-request input.

## Repository controls

- GitHub Actions are pinned to full commit SHAs.
- The .NET SDK version and NuGet dependency graph are locked in source control.
- CI, CodeQL, dependency review, catalog validation, metadata validation, and release tests run from public workflow definitions.
- Workflow, script, catalog, SDK, lock-file, and signing-policy paths are covered by CODEOWNERS.
- Every signing request requires a separate approval; pushing a tag alone is not signing authorization.

The normative project policy is [Code signing policy](CODE_SIGNING_POLICY.md). SignPath Foundation's current conditions remain authoritative if they require stricter configuration.
