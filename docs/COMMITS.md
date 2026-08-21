# Commity a záznam změn

## Formát commit message

Používej Conventional Commits:

```text
<typ>(<oblast>): stručný rozkazovací popis
```

Příklady:

```text
docs(architecture): define portable runtime boundaries
feat(supervisor): add Apache process health check
fix(paths): resolve instance data after drive move
test(portability): cover relative configuration paths
```

Povolené typy: `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, `chore`.

## Kdy aktualizovat záznamy

- `CHANGELOG.md`: změna, kterou pocítí uživatel nebo vydání.
- `docs/WORKLOG.md`: významný pracovní krok, ověření, omezení či další konkrétní práce.
- `docs/DECISIONS.md`: technická volba se širším dopadem.

Jeden commit má řešit jednu věc. Nezahrnuj do něj stažené runtime balíčky, uživatelská data, logy ani lokální IDE nastavení.
