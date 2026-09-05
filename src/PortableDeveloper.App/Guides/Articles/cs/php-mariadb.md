Výchozí lokální účet je root bez hesla a první databáze je portable_dev. Pokud jste údaje změnili, upravte je také ve skriptu.

```php
<?php
$db = new mysqli(
    '127.0.0.1',
    'root',
    '',
    'portable_dev',
    {{MARIADB_PORT}}
);
$db->set_charset('utf8mb4');

$rows = $db->query('SELECT NOW() AS server_time');
echo $rows->fetch_assoc()['server_time'];
```
