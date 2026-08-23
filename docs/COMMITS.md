# Commit conventions

Use focused commits with an imperative Conventional Commit subject:

```text
feat(selenium): add editable immutable profiles
fix(packages): repair incomplete fixed-target installs
docs(release): prepare 1.0.0
test(storage): cover protected data boundaries
```

Preferred types are `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, and `chore`. Keep generated binaries, caches, profiles, databases, secrets, and personal paths out of commits. A release commit may combine the already-tested product changes, version metadata, changelog, and release documentation that define one public version.
