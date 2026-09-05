These guides apply to the environment managed by the application. Examples use the current ports from Port Manager and work without the system PATH, Docker, or a browser installed in Windows.

> Guides are bundled with this application version and work offline. Always copy profile and cookie-vault IDs from the application UI.

1. Install Selenium and at least one complete browser pack in Modules.
2. Start Selenium Server.
3. For Python, install the runtime and add the direct selenium package on the Python page.
4. For PHP, install Composer and add php-webdriver/webdriver to the active project.
5. If you use a master profile or cookie vault, copy its ID from its Selenium card.

Projects are shared workspaces. On the Projects tab, select an item in the list to see its tools and web settings in one detail panel. The web root, Apache enablement, and `.htaccess` are saved together; apply saved changes to a running Apache instance with the separate restart action. Enabling web support creates a default `index.html` when one does not exist, so the starter page also works without PHP.

Portable Python is intentionally a clean runtime. The selenium library is not part of the base module; installing it explicitly keeps the environment smaller and predictable.

### Current local endpoints

- Apache: http://127.0.0.1:{{APACHE_PORT}}
- MariaDB: 127.0.0.1:{{MARIADB_PORT}}
- Selenium: http://127.0.0.1:{{SELENIUM_PORT}}
