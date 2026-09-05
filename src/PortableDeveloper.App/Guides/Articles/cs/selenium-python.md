Ukázka používá spravovaný Firefox. Pro Chrome změňte import Options na selenium.webdriver.chrome.options. PROFILE_ID nahraďte hodnotou zkopírovanou z aplikace.

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

Relaci vždy ukončete pomocí quit(), ideálně v bloku finally. Aplikace pak může odstranit její dočasnou pracovní kopii profilu.
