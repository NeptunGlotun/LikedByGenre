# ====== Identity BUSINESS LOGIC ======

## Purpose

`Identity` володіє:
- локальною ідентифікацією та автентифікацією користувачів через логін та пароль;
- безпечним хешуванням та зберіганням паролів у базі даних за допомогою алгоритму **BCrypt** з індивідуальною сіллю (workFactor = 12);
- федеративною автентифікацією власника Spotify-акаунта через **OAuth 2.0 з PKCE**;
- cookie-сесією ASP.NET Core (`LikedByGenre.Auth`);
- керуванням станом автентифікованого суб'єкта через Claims (`NameIdentifier`, `Name`);
- видачею access token для викликів Spotify Web API (через `CurrentUser`).

**NOT here:**
- імпорт треків — `Library`;
- класифікація — `Classification`;
- створення плейлистів — `Playlists`;
- зберігання відкритих паролів — суворо заборонено політикою безпеки.

## Entities

| Entity | Basic Fields | Description | Invariants |
|---|---|---|---|
| `User` *(Local Entity)* | Id, Username, PasswordHash, SpotifyId, CreatedAt | Локальний обліковий запис у базі даних SQL Server. | - `Username` унікальний та не порожній.<br>- `PasswordHash` є валідним рядком формату `$2a$12$...` (BCrypt). Відкритий пароль ніколи не персиститься.<br>- `CreatedAt` іммутабельний UTC timestamp. |
| Сесія cookie `LikedByGenre.Auth` | NameIdentifier, Name, access_token, refresh_token | Стан активного користувача в додатку. | - Без валідної cookie захищені сторінки недоступні.<br>- Токени Spotify зберігаються лише у захищеній cookie сесії, а не в базі даних. |

## Сценарії автентифікації

### 1. Локальна реєстрація та вхід (BCrypt)
1. Користувач вводить `Username` та `Password` на `/Account/Register`.
2. Валідація довжини пароля (>= 8 символів) та унікальності імені.
3. Генерація хешу: `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)` (автоматичне створення криптографічної солі).
4. Збереження `User` у SQL Server.
5. При вході (`/Account/Login` POST) виконується перевірка `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)`.
6. У разі невідповідності повертається узагальнене повідомлення *"Неправильний логін або пароль"* для захисту від enumeration атак.

### 2. Федеративний вхід через Spotify OAuth 2.0 PKCE
1. Виклик `ChallengeSpotify` схеми `Spotify`.
2. Обмін Authorization Code на токени доступу на `/signin-spotify`.
3. Запис claims та токенів у захищену cookie-сесію.

## Application Commands & Queries

**Commands:**

| Area | Commands |
|---|---|
| Локальний доступ | `RegisterUser(username, password)`, `LoginUser(username, password)` |
| Сесія | `ChallengeSpotify`, `Logout` (POST) |

**Queries:**

| Query | Purpose |
|---|---|
| `GetUserByUsername` | Пошук облікового запису для перевірки хешу пароля |
| `Claims` поточного користувача | Ідентифікатор і ім'я для UI та фільтрації даних |
| `GetAccessTokenAsync` | Bearer-токен для викликів Spotify API |

## Infrastructure

- Бібліотека хешування: `BCrypt.Net-Next` (версія 4.2.0, алгоритм 2a).
- Схема автентифікації: Cookie + OAuth (`Program.cs`).
- Cookie: HttpOnly, SameSite=Lax, SecurePolicy = SameAsRequest, 8 годин, sliding expiration.
- Scopes Spotify: `user-library-read`, `playlist-modify-private`, `playlist-modify-public`, `user-read-private`.
