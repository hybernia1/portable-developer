# Selenium cookie vault

The cookie vault imports a JSON cookie export without requiring a password. Import is local and accepts a top-level array or a common wrapper containing a cookie array. Extension metadata is discarded.

Retained fields are cookie name, value, domain, path, expiry, `httpOnly`, `secure`, and `sameSite`. The importer canonicalizes domains and SameSite values, rejects invalid or expired records, removes duplicates deterministically, and never writes cookie values to logs.

Values are encrypted with AES-256-GCM using a random key generated in `state/selenium-cookie-vault.key`. A vault envelope under `profiles/selenium-vaults/<id>/vault.json` keeps only the UI metadata, nonce, ciphertext, and authentication tag. There is no plaintext temporary payload.

At session creation, the portable Java Selenium node decrypts the selected vault in memory, visits the required origins, and inserts compatible cookies through WebDriver. The application never sends the vault to its maintainers.

Because the encryption key is stored in the same portable root, this protects separately copied vault files and detects modification; it does not protect a stolen complete application folder. Cookies can be equivalent to credentials. Keep `profiles/`, `state/`, and original exports private, and revoke sessions after suspected disclosure.
