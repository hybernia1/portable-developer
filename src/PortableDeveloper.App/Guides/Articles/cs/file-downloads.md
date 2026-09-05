Vlastní download adresář nenastavujte ve Firefox nebo Chrome options. Nejdříve povolte stahování v nastavení Selenium. Server potom uloží soubory do složky seldownloads aktivního projektu nezávisle na profilu a relaci.

```python
from pathlib import Path

project_root = Path(__file__).resolve().parent
downloads = project_root / "seldownloads"

for downloaded_file in downloads.iterdir():
    print(downloaded_file.name)
```

Obsah seldownloads je trvalý uživatelský obsah. Ukončení relace jej nemaže a Apache k této složce nemá přístup.
