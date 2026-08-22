# Vývojové prostředí

## Požadavky

- Windows 10 nebo 11 x64;
- .NET SDK 10.0.400 podle `global.json`;
- Git a PowerShell;
- pro první release build připojení k internetu; další build lze provést z ověřené lokální cache.

Koncový uživatel tyto nástroje nepotřebuje. Distribuce je self-contained a obsahuje servery i runtime.

## Běžný cyklus

```powershell
dotnet restore
dotnet build PortableDeveloper.slnx
dotnet test PortableDeveloper.slnx --configuration Release
& .\scripts\Publish-Windows.ps1
```

Výchozí release skript spustí `Fetch-Dependencies.ps1`, který načte přesné verze, URL a SHA-256 z `catalog/dependencies.lock.json`. Stáhne Apache, PHP, MariaDB, Selenium, geckodriver, OpenJDK, Composer, Python, Notepad++, phpMyAdmin a Microsoft VC++ Redistributable do `downloads/dependencies/`. Laragon ani ruční doplňování souborů není potřeba.

Pro samostatné stažení nebo kontrolu cache lze použít:

```powershell
& .\scripts\Fetch-Dependencies.ps1
& .\scripts\Fetch-Dependencies.ps1 -ValidateCatalogOnly
& .\scripts\Fetch-Dependencies.ps1 -VerifyOnly
& .\scripts\Publish-Windows.ps1 -OfflineDependencies
```

Režim `-ValidateCatalogOnly` kontroluje schéma, unikátní ID, názvy souborů a povolené HTTPS zdroje bez vytvoření cache. Režimy `-VerifyOnly` a `-OfflineDependencies` nic nestahují a při chybějícím či změněném souboru skončí chybou. `downloads/`, `artifacts/` a runtime data jsou ignorované Gitem; binárky se do repozitáře necommitují.

Skript vytvoří `artifacts/publish/PortableDeveloper-offline-win-x64/`. Pokud složka existuje, skončí chybou; zvol nový `-OutputPath` nebo existující release archivuj ručně. Bezpečnostní pojistka zabraňuje přepsání dat vytvořených při spuštění portable aplikace. Po úspěšném publishi `Cleanup-Releases.ps1` ponechá dva nejnovější release adresáře a každý adresář s běžícím procesem; počet lze změnit parametrem `-ReleasesToKeep`.

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
10. Composer 2.10.2, Python 3.13.0 a editor Notepad++ 8.9.2 odpovídají `.portable-developer-tool.json`; základní Python obsahuje jen pip a žádné knihovny z build profilu.
11. Editor neobsahuje updater, pluginy, session, zálohy ani jiné uživatelské soubory ze zdrojového prostředí a má pouze českou lokalizaci vedle vestavěné angličtiny.
12. Vestavěný správce souborů, terminál a Composer sledují stejný aktivní projekt; chrání jeho kořen a nepřistupují k ostatním projektům přes relativní únikovou cestu.
13. Kořen obsahuje `PortableDeveloper.exe` a pouze nutné nativní WPF DLL; neobsahuje volné spravované .NET DLL, PDB ani zdrojové varianty `php.ini*`.
14. Apache konfigurace obsahuje Default na `localhost`, všechny zapnuté `<id>.localhost` hosty, lokální omezení přístupu a očekávané `AllowOverride` pro každý projekt.

## Veřejná CI

Workflow `.github/workflows/ci.yml` běží na Windows pro každý pull request a push do `main`. Použije SDK připnuté v `global.json`, obnoví závislosti, ověří formátování, sestaví řešení v konfiguraci Release a spustí testy.

CI záměrně zatím nevytváří téměř gigabajtový offline balík. Všechny jeho vstupy jsou už reprodukovatelně stažitelné a hashově připnuté; před automatizovaným veřejným releasem zbývá dokončit licenční audit, vytvoření release archivu a oddělený podpis pouze vlastního EXE.
