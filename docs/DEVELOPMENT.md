# Development

## Prerequisites

- Windows 10/11 x64
- .NET SDK version pinned by `global.json`
- PowerShell 7 or Windows PowerShell for repository scripts

No local Laragon installation or system runtime is a build input.

## Standard verification

```powershell
dotnet restore PortableDeveloper.slnx --locked-mode
dotnet format PortableDeveloper.slnx --verify-no-changes --no-restore
dotnet build PortableDeveloper.slnx --configuration Release --no-restore
dotnet test PortableDeveloper.slnx --configuration Release --no-build --no-restore
.\scripts\Fetch-Dependencies.ps1 -ValidateCatalogOnly
```

Use `dotnet run --project src/PortableDeveloper.App` for development. Runtime data then stays under that output's portable root; never point development code at user release data.

## Packages and publishing

`catalog/dependencies.lock.json` is authoritative. Every entry needs an exact version, HTTPS source, archive SHA-256, normalized entrypoint path and SHA-256, license information, and a package-specific validation rule where applicable. Do not update a hash without independently identifying and verifying the upstream artifact.

The public-style build is:

```powershell
.\scripts\Publish-Online-Windows.ps1 -Version 1.22.1
```

The script records the full source revision, generates an SPDX 2.2 SBOM, and produces a ZIP plus checksum. NuGet dependencies are committed in `packages.lock.json`; dependency changes must intentionally update and review those files. GitHub Actions are pinned to full commit SHAs.

The full offline aggregate requires the dependency cache populated by `Fetch-Dependencies.ps1` and is subject to a separate redistribution review.

## Change discipline

- Keep portable paths relative in persisted data.
- Add tests for path boundaries, process ownership, parsers, catalogs, package rollback, and lifecycle changes.
- Add user-visible changes to `CHANGELOG.md` and architectural decisions to `docs/DECISIONS.md`.
- Do not commit build output, archives, runtime caches, modules, profiles, databases, logs, or secrets.
