portable:profile loads a complete immutable master profile. It contains sign-ins, extensions, bookmarks, and other browser state. Every session uses its own temporary copy and never writes back to the master. A profile must match the managed browser type.

portable:vault injects normalized cookies only. It is lighter and suitable for one sign-in without carrying a complete profile. A vault does not require a browser account or cloud synchronization and can be used alone or together with a master profile. It still needs valid exported cookies; if the website revokes the session or the cookies expire, import a fresh vault.

```python
options.set_capability("portable:profile", "PROFILE_ID")
options.set_capability("portable:vault", "VAULT_ID")
```

A profile or vault name is not its capability ID. Use Copy ID on the corresponding card.
