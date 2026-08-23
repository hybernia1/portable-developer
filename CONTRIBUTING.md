# Contributing

Read [Architecture](docs/ARCHITECTURE.md) and [Portability](docs/PORTABILITY.md) before changing code.

By contributing, you confirm that you may publish the contribution and license it under [GPL-3.0-or-later](LICENSE). Copyright remains with each contributor; no CLA or copyright assignment is required.

Before submitting a change:

- update documentation when behavior or design changes;
- add user-visible changes to `CHANGELOG.md`;
- run formatting, build, and relevant tests;
- never commit content from `modules/`, `downloads/`, `instances/`, `profiles/`, `logs/`, `cache/`, or `temp/`.

Use the conventions in [docs/COMMITS.md](docs/COMMITS.md).

```powershell
dotnet restore PortableDeveloper.slnx
dotnet format PortableDeveloper.slnx --verify-no-changes --no-restore
dotnet build PortableDeveloper.slnx --configuration Release --no-restore
dotnet test PortableDeveloper.slnx --configuration Release --no-build --no-restore
```

Public GitHub Actions CI runs the same checks on pull requests and pushes to `main`.
