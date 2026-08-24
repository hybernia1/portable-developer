# Portable Developer in practice

These guides apply to the environment managed by the application. Examples use the current ports from Port Manager and work without the system PATH, Docker, or a browser installed in Windows.

> Guides are bundled with this application version and work offline. Always copy profile and cookie-vault IDs from the application UI.

## Chapters

1. Environment preparation and local endpoints
2. Selenium with Python
3. Selenium with PHP
4. Master profiles and cookie vaults
5. File downloads
6. PHP with MariaDB
7. Portable rules for your scripts
8. Interactive portable terminal

## 1. Environment preparation

Tags: quick start, modules, selenium

1. Install Selenium and at least one complete browser pack in Modules.
2. Start Selenium Server.
3. For Python, install the runtime and add the direct selenium package on the Python page.
4. For PHP, install Composer and add php-webdriver/webdriver to the active project.
5. If you use a master profile or cookie vault, copy its ID from its Selenium card.

Portable Python is intentionally a clean runtime. The selenium library is not part of the base module; installing it explicitly keeps the environment smaller and predictable.

### Current local endpoints

- Apache: http://127.0.0.1:{{APACHE_PORT}}
- MariaDB: 127.0.0.1:{{MARIADB_PORT}}
- Selenium: http://127.0.0.1:{{SELENIUM_PORT}}

## 2. Selenium with Python

Tags: selenium, python, master profile

This example uses managed Firefox. For Chrome, import Options from selenium.webdriver.chrome.options. Replace PROFILE_ID with the value copied from the application.

```python
from selenium import webdriver
from selenium.webdriver.firefox.options import Options

options = Options()
options.set_capability("portable:profile", "PROFILE_ID")

driver = webdriver.Remote(
    command_executor="http://127.0.0.1:{{SELENIUM_PORT}}",
    options=options,
)
try:
    driver.get("https://example.com/")
    print(driver.title)
finally:
    driver.quit()
```

Always end the session with quit(), preferably in a finally block. The application can then remove its temporary profile working copy.

## 3. Selenium with PHP

Tags: selenium, php, composer

First add php-webdriver/webdriver on the Composer page. The package and vendor directory stay with the active project.

```php
<?php
require __DIR__ . '/vendor/autoload.php';

use Facebook\WebDriver\Remote\DesiredCapabilities;
use Facebook\WebDriver\Remote\RemoteWebDriver;

$capabilities = DesiredCapabilities::firefox();
$capabilities->setCapability('portable:profile', 'PROFILE_ID');

$driver = RemoteWebDriver::create(
    'http://127.0.0.1:{{SELENIUM_PORT}}',
    $capabilities
);
try {
    $driver->get('https://example.com/');
    echo $driver->getTitle();
} finally {
    $driver->quit();
}
```

## 4. Master profiles and cookie vaults

Tags: selenium, master profile, vault, cookies

portable:profile loads a complete immutable master profile. It contains sign-ins, extensions, bookmarks, and other browser state. Every session uses its own temporary copy and never writes back to the master. A profile must match the managed browser type.

portable:vault injects normalized cookies only. It is lighter and suitable for one sign-in without carrying a complete profile. A vault does not require a browser account or cloud synchronization and can be used alone or together with a master profile. It still needs valid exported cookies; if the website revokes the session or the cookies expire, import a fresh vault.

```python
options.set_capability("portable:profile", "PROFILE_ID")
options.set_capability("portable:vault", "VAULT_ID")
```

A profile or vault name is not its capability ID. Use Copy ID on the corresponding card.

## 5. File downloads

Tags: selenium, downloads, files

Do not set a custom download directory in Firefox or Chrome options. Enable downloads in Selenium settings first. The server then stores files in the active project's seldownloads directory independently of the profile and session.

```python
from pathlib import Path

project_root = Path(__file__).resolve().parent
downloads = project_root / "seldownloads"

for downloaded_file in downloads.iterdir():
    print(downloaded_file.name)
```

The seldownloads directory is persistent user content. Ending a session does not delete it, and Apache cannot serve it.

## 6. PHP and MariaDB

Tags: php, mariadb, database

The default local account is root without a password and the initial database is portable_dev. If you changed these values, update the script too.

```php
<?php
$db = new mysqli(
    '127.0.0.1',
    'root',
    '',
    'portable_dev',
    {{MARIADB_PORT}}
);
$db->set_charset('utf8mb4');

$rows = $db->query('SELECT NOW() AS server_time');
echo $rows->fetch_assoc()['server_time'];
```

## 7. Portable rules for your scripts

Tags: portable, security, paths

- Use 127.0.0.1 and ports from Port Manager.
- Do not rely on the system PATH or a host browser.
- Build paths relative to the project instead of using a fixed drive letter.
- Do not write sensitive IDs to public logs or commit profiles, vaults, or database data.
- End long-running operations cleanly so browser and session working copies are not left behind.

## 8. Interactive portable terminal

Tags: terminal, python, php, portable

Bundled Python and PHP programs can print incremental output and read one line at a time directly in the application terminal. This includes scripts using Python `input()` or PHP standard input. Press Enter to send the current line. Press Ctrl+C with no selected text to stop the running process and its owned child processes.

Python runs in UTF-8 and unbuffered mode, so Unicode text and prompts without a trailing newline appear immediately. The terminal deliberately does not expose `cmd.exe`, PowerShell, arbitrary executables, pipes, redirects, or shell chaining.

Type `help` for the complete command list. Safe project-local commands include `ls`, `find`, `grep`, `tree`, `cd`, `mkdir`, `cat`, `touch`, `write`, `cp`, `mv`, `rm`, `rmdir`, and `echo`. `grep` reads only UTF-8 files up to 1 MiB, while `find` and `tree` cap their output. `write` creates a new UTF-8 file and never replaces an existing one. Deletion is limited to one file or one empty directory at a time; recursive deletion and paths outside the active project are blocked.

Install and remove Python packages only from the Python page. The terminal rejects `python -m pip` and `python -m ensurepip` so the verified Python runtime and portable package registry stay consistent. Project Python and PHP code still run with the current Windows user's permissions; the terminal is a project-boundary aid, not an operating-system sandbox.
