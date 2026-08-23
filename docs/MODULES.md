# Module layout

Online and offline distributions use normalized, versioned directories. A clean online installation may contain none of the optional paths.

```text
modules/
  apache/2.4.68/bin/httpd.exe
  php/8.4.12/php-cgi.exe
  mariadb/12.3.2/bin/mariadbd.exe
  selenium/4.47.0/selenium-server.jar
  jre/25.0.3/bin/java.exe
  composer/2.10.2/composer.phar
  python/3.13.0/python.exe
  editor/8.9.2/notepad++.exe
  browsers/chrome-for-testing/152.0.7977.54/chrome.exe
  browsers/firefox/154.0/firefox.exe
drivers/bundled/
  drivers.json
  chrome/152.0.7977.54/chromedriver.exe
  firefox/0.37.1/geckodriver.exe
profiles/
  selenium/<id>/profile.json
  selenium/<id>/master/
  selenium-vaults/<id>/vault.json
state/
  selenium-cookie-vault.key
```

Server inventory requires the exact catalog version, safe relative path, metadata, and entrypoint SHA-256. Tool inventory applies equivalent checks through `.portable-developer-tool.json`. Browser readiness requires an exact catalog browser/driver pair; a system or user-supplied executable cannot be paired with a managed component.

Python project packages live under `instances/default/python/packages`, not the immutable base interpreter. The editor keeps local configuration beside its executable and does not register associations. The file manager is built into the application.
