Tyto návody platí pro prostředí spravované aplikací. Ukázky používají aktuální porty z Port Manageru a fungují bez systémového PATH, Dockeru nebo browseru nainstalovaného ve Windows.

> Návody jsou součástí konkrétní verze aplikace a fungují offline. ID profilů a cookie vaultů vždy kopírujte z rozhraní aplikace.

1. V Modulech nainstalujte Selenium a alespoň jeden kompletní browser pack.
2. Spusťte Selenium Server.
3. Pro Python nainstalujte runtime a na stránce Python přidejte přímý balíček selenium.
4. Pro PHP nainstalujte Composer a v aktivním projektu přidejte php-webdriver/webdriver.
5. Pokud používáte master profil nebo cookie vault, zkopírujte jeho ID z karty v Selenium.

Projekty jsou společné pracovní prostory. V záložce Projekty vyberte položku ze seznamu a její nástroje i webové nastavení najdete v jediném detailu vpravo. Webový kořen, zapnutí v Apache a `.htaccess` se ukládají společně; změny běžícího Apache použijte samostatným tlačítkem pro restart. Při zapnutí webové podpory aplikace vytvoří výchozí `index.html`, pokud ještě neexistuje, takže úvodní stránka funguje i bez PHP.

Portable Python je záměrně čistý runtime. Knihovna selenium není součástí základního modulu a její explicitní instalace udržuje prostředí menší a předvídatelné.

### Aktuální lokální endpointy

- Apache: http://127.0.0.1:{{APACHE_PORT}}
- MariaDB: 127.0.0.1:{{MARIADB_PORT}}
- Selenium: http://127.0.0.1:{{SELENIUM_PORT}}
