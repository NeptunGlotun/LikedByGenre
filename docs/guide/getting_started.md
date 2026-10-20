# Getting started

## Що потрібно локально

- Visual Studio з workload ASP.NET and web development
- .NET 8
- SQL Server (локальний екземпляр) і SSMS
- Spotify Premium у власника Developer App (вимога Development Mode Spotify)
- акаунт на [developer.spotify.com/dashboard](https://developer.spotify.com/dashboard)

## Spotify Dashboard (додаток SSS)

Додаток **SSS (Simple Songs Sorting)** вже зареєстровано в [Spotify Developer Dashboard](https://developer.spotify.com/dashboard):
- **Client ID:** `cb169528d7bd43f9a3614046f6bae9d1`
- **User Management:** тестовий користувач `Neptun_Glotun` уже доданий.
- **Redirect URIs:** Spotify вимагає для локального тестування використання саме IP-адреси `127.0.0.1` та протоколу `http://` (без SSL, оскільки Spotify вважає `https://localhost` небезпечним `Insecure`). Додайте у список Redirect URIs:
  - `http://127.0.0.1:5164/signin-spotify`
  *(Обов'язково збережіть зміни кнопкою Save внизу сторінки).*

## Секрети (User Secrets)

У Package Manager Console або в терміналі проєкту (`src/LikedByGenre`):

```bash
dotnet user-secrets set "Spotify:ClientId" "cb169528d7bd43f9a3614046f6bae9d1"
dotnet user-secrets set "Spotify:ClientSecret" "<значення_Client_Secret_з_Dashboard>"
```

Значення Client Secret береться у вкладці *Basic Information* за посиланням *View client secret*.
Не записувати ці значення в `appsettings.json` і не комітити їх у Git.


## База даних

Рядок за замовчуванням: `Server=localhost;Database=LikedByGenre;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`

При першому старті `EnsureCreated()` створює базу `LikedByGenre` і таблиці. Перевірка — у SSMS.

Якщо екземпляр називається `SQLEXPRESS`, замінити `localhost` на `.\SQLEXPRESS` (див. `configuration.md`).

## Запуск

1. Відкрити `LikedByGenre.sln`.
2. Переконатися, що SQL Server запущений.
3. F5 (профіль `LikedByGenre`, HTTPS `https://localhost:7164`).
4. Увійти через Spotify.
5. На сторінці «Бібліотека» спочатку завантажити треки, потім кілька разів підтягнути жанри артистів.
6. Переглянути групи й за потреби створити плейлисти.
