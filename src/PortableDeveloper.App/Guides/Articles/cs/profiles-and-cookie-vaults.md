portable:profile načte kompletní neměnný master profil. Obsahuje přihlášení, rozšíření, záložky a další stav browseru. Každá relace používá vlastní dočasnou kopii a nikdy nezapisuje zpět do masteru. Profil musí patřit stejnému typu spravovaného browseru.

portable:vault vloží pouze normalizované cookies. Je lehčí a vhodný pro jedno přihlášení bez přenosu celého profilu. Vault nepotřebuje účet v prohlížeči ani cloudovou synchronizaci a lze jej použít samostatně nebo společně s master profilem. Potřebuje však platné exportované cookies; pokud web relaci zruší nebo cookies vyprší, je nutné vault znovu importovat.

```python
options.set_capability("portable:profile", "PROFILE_ID")
options.set_capability("portable:vault", "VAULT_ID")
```

Název profilu ani vaultu není jeho capability ID. Použijte tlačítko Kopírovat ID na příslušné kartě.
