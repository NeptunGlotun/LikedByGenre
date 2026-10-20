# Configuration

Конфігурація читається з `appsettings.json`, `appsettings.Development.json` і User Secrets. Пріоритет вищий у User Secrets.

## ConnectionStrings:DefaultConnection

Використовується EF Core для SQL Server. Windows Authentication (`Trusted_Connection=True`). Окремого логіна SQL у додатку немає.

Альтернативи, якщо `localhost` не підходить:

- `Server=.\\SQLEXPRESS;Database=LikedByGenre;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`
- `Server=(localdb)\\mssqllocaldb;Database=LikedByGenre;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`

## Spotify:ClientId / Spotify:ClientSecret

Ідентифікатор і секрет додатка з Spotify Developer Dashboard. Зберігати в User Secrets.

Redirect URI зашитий у код як callback path `/signin-spotify` відносно хоста запуску. Для локального профілю VS це `http://127.0.0.1:5164/signin-spotify` (використовується loopback IP `127.0.0.1` та протокол `http://` за вимогами безпеки Spotify OAuth). URI в Dashboard має збігатися символ у символ.

## Logging

У продакшен-режимі стек винятків користувачеві не показується (`UseExceptionHandler`). Access token і refresh token у логи не пишуться.
