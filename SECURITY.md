# Security

## Supported versions

Security fixes are provided for the latest released series.

| Version | Supported |
|---|---|
| 1.0.x | Yes |
| < 1.0 | No |

## Reporting a vulnerability

Do not open a public issue for a sensitive vulnerability. Use [GitHub private vulnerability reporting](https://github.com/hybernia1/portable-developer/security/advisories/new) and include the affected version, reproduction steps, expected impact, and any proposed mitigation. Ordinary bugs belong in [GitHub Issues](https://github.com/hybernia1/portable-developer/issues).

## Security boundaries

Portable Developer keeps its own state under one root but is not an OS-enforced sandbox. PHP, Python, Composer packages, Selenium tests, and other user code run with the current Windows user's permissions. Run only trusted code and packages.

The runtime downloader does not accept arbitrary URLs. Trust is anchored in the release's local catalog, allowlisted HTTPS origins, pinned archive and entrypoint SHA-256 hashes, safe extraction, and atomic installation. Report a suspected compromised upstream archive, incorrect hash, extraction traversal, or reparse-point escape privately.

Cookie vaults use AES-256-GCM with a key stored in portable `state/`. They do not create plaintext temporary payloads, but they cannot protect against an attacker who obtains the entire portable folder. Browser masters under `profiles/` can likewise contain live sessions and credentials. Never attach either to issues or test fixtures. Revoke affected sessions after suspected exposure.

Files in project `seldownloads` are persistent, untrusted downloads. Verify their origin before opening or executing them.
