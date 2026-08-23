# Native runtime dependencies

Portable Developer does not depend on a globally installed Visual C++ Redistributable, Java, Python, or browser.

Apache and PHP receive an app-local set of Microsoft `vcruntime140*` and `msvcp140*` DLLs. Packaging extracts them without installation from the exact signed Microsoft redistributable, verifies version `14.51.36247.0`, Microsoft signer identity, and individual SHA-256 values, then records `.portable-developer-runtime.json`. Runtime preflight verifies them again before service start.

Selenium uses Microsoft OpenJDK 25.0.3 from `modules/jre/25.0.3`. The controller invokes its explicit `java.exe`; Selenium Manager is disabled. Browsers and drivers are optional catalog-matched packages.

Python 3.13.0 is installed under `modules/python/3.13.0` with pip 24.2. User packages stay under `instances/default/python/packages`; user site-packages and global pip configuration are disabled.

Notepad++ 8.9.2 is a verified portable tool under `modules/editor/8.9.2`, with local configuration and without updater, session, backup, or system association changes.

Downloaded binaries and Microsoft DLLs are not stored in Git. Release scripts obtain exact pinned upstream inputs in ignored local cache. Third-party license and NOTICE files must be retained.
