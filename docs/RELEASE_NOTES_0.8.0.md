# Portable Developer 0.8.0

Verze 0.8.0 sjednocuje aplikační UI a mění Selenium z evidence osamocených driverů na kontrolovaná browser prostředí.

## Nejdůležitější změny

- Jedna spuštěná instance aplikace; další start obnoví a aktivuje existující okno.
- Vlastní tmavá horní lišta hlavního okna i aplikačních dialogů a jednotný styl všech selectů.
- Běžné soubory se otevírají přes asociace Windows, volitelný Notepad++ už není podmínkou editace konfigurace.
- Doporučené Selenium prostředí tvoří ověřený portable Chrome for Testing 152.0.7977.54 a přesně odpovídající ChromeDriver.
- Systémový Edge, Chrome nebo Firefox se pouze detekuje; Windows instalace se nemění a nekompatibilní driver server nespustí.
- Čistý master profil vzniká v dočasné složce aplikace. Chromium i Firefox profily se normalizují, zbaví cache a locků, omezí velikostí a zapečetí hashovaným manifestem.

## Bezpečnost a přenositelnost

Stažené browser prostředí používá verzovaný katalog, omezený HTTPS host a dva SHA-256 kontrolní body. Grid i prohlížeče dostávají explicitní cesty a aplikace nemění systémový `PATH`, registr ani služby. Přihlašovací údaje importované ze systémového browser profilu nemusí být přenositelné na jiný účet nebo počítač kvůli Windows šifrování; doporučenou cestou je proto čistý master vytvořený přímo v Portable Developer.

Veřejný release obsahuje self-contained Windows x64 ZIP a jeho SHA-256 součet. EXE zůstává nepodepsaný, dokud není dokončený code-signing proces; ochranu Windows kvůli aplikaci nevypínejte.
