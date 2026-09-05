Do not set a custom download directory in Firefox or Chrome options. Enable downloads in Selenium settings first. The server then stores files in the active project's seldownloads directory independently of the profile and session.

```python
from pathlib import Path

project_root = Path(__file__).resolve().parent
downloads = project_root / "seldownloads"

for downloaded_file in downloads.iterdir():
    print(downloaded_file.name)
```

The seldownloads directory is persistent user content. Ending a session does not delete it, and Apache cannot serve it.
