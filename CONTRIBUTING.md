# Přispívání

Než začneš měnit kód, přečti si [AGENTS.md](AGENTS.md), [architekturu](docs/ARCHITECTURE.md) a [pravidla portability](docs/PORTABILITY.md).

Odesláním příspěvku potvrzuješ, že jej smíš zveřejnit a poskytuješ jej pod stejnou licencí [GPL-3.0-or-later](LICENSE) jako projekt. Autorská práva zůstávají přispěvateli; projekt nevyžaduje CLA ani jejich převod.

## Před odevzdáním změny

- aktualizuj dokumentaci, pokud se změnil návrh nebo chování;
- přidej položku do `CHANGELOG.md`, pokud je změna uživatelsky viditelná;
- zaznamenej významný krok do `docs/WORKLOG.md`;
- spusť odpovídající formátování, build a testy;
- nikdy nepřidávej obsah složek `modules/`, `downloads/`, `instances/`, `logs/`, `cache/` a `temp/`.

Používej konvence v [docs/COMMITS.md](docs/COMMITS.md).

## Ověření změny

```powershell
dotnet restore PortableDeveloper.slnx
dotnet format PortableDeveloper.slnx --verify-no-changes --no-restore
dotnet build PortableDeveloper.slnx --configuration Release --no-restore
dotnet test PortableDeveloper.slnx --configuration Release --no-build --no-restore
```

Stejné kontroly spouští veřejná GitHub Actions CI pro každý pull request a push do `main`.
