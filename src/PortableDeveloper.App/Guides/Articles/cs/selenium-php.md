Nejprve na stránce Composer přidejte php-webdriver/webdriver. Balíček i adresář vendor zůstanou u aktivního projektu.

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
