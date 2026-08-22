# Vývojové prostředí

## Požadavky

- Windows 10 nebo 11 x64;
- .NET SDK 10.0.400 podle `global.json`;
- Git a PowerShell;
- pro offline release ověřené lokální zdroje komponent.

Koncový uživatel tyto nástroje nepotřebuje. Distribuce je self-contained a obsahuje servery i runtime.

## Běžný cyklus

```powershell
dotnet restore
dotnet build PortableDeveloper.slnx
dotnet test PortableDeveloper.slnx --configuration Release
& .\scripts\Publish-Windows.ps1
```

Výchozí release skript očekává:

- `E:\laragon\bin` s Apache 2.4.66, PHP 8.4.12, DBeaver JRE 25.0.3 a Pythonem 3.13.0;
- `downloads/bundle-cache/composer-2.10.2.phar` s připnutým SHA-256;
- `downloads/mariadb-12.3.2-winx64.zip` s připnutým SHA-256;
- `downloads/bundle-cache/selenium-server-4.47.0.jar` s připnutým SHA-256;
- `downloads/bundle-cache/geckodriver-v0.37.1-win64.zip` s připnutým SHA-256;
- podepsané Microsoft VC++ x64 DLL v explicitním build zdroji (výchozí `System32`).

`downloads/`, `artifacts/` a runtime data jsou ignorované Gitem. Serverové binárky se do repozitáře necommitují.

Skript vytvoří `artifacts/publish/PortableDeveloper-offline-win-x64/`. Pokud složka existuje, skončí chybou; zvol nový `-OutputPath` nebo existující release archivuj ručně. Bezpečnostní pojistka zabraňuje přepsání dat vytvořených při spuštění portable aplikace.

## Kontrola před vydáním

1. Všechny testy procházejí v Release konfiguraci.
2. `bundle-manifest.json` neobsahuje lokální absolutní zdrojové cesty.
3. Dashboard zobrazuje Apache, PHP a MariaDB jako připravené a Selenium se spustí s ověřeným geckodriverem.
4. Verze komponent lze spustit explicitně z jejich složek bez systémového `PATH`.
5. Aplikace běží pod standardním uživatelem a z cesty s mezerami.
6. Po přesunu na jiné písmeno disku se regeneruje transientní konfigurace.
7. Stop i zavření aplikace ukončí všechny vlastněné podprocesy.
8. Release obsahuje požadované licence a notices; veřejná redistribuce prošla licenční kontrolou.
9. `drivers/bundled/drivers.json` odpovídá SHA-256 přibaleného driveru a `drivers/custom/` je prázdná připravená složka.
10. Composer 2.10.2 a Python 3.13.0 odpovídají `.portable-developer-tool.json`; základní Python obsahuje jen pip a žádné knihovny z build profilu.
