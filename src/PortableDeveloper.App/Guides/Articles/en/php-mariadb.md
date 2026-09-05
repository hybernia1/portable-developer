The default local account is root without a password and the initial database is portable_dev. If you changed these values, update the script too.

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
