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
