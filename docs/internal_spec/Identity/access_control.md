# Модель керування доступом — Identity Context

## 1. Обрана модель
**Змішана модель:** Поєднання рольової (RBAC) та дискреційної (DAC / Ownership-Based) моделей.

## 2. Суб'єкти системи
| Суб'єкт | Опис | Ключові Claims |
|---|---|---|
| **Anonymous** | Неавтентифікований відвідувач | Відсутні |
| **User** | Звичайний користувач | `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role = "User"` |
| **Admin** | Системний адміністратор | `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role = "Admin"` |

## 3. Матриця операцій та дозволів

| Операція | Ресурс | Anonymous | User | Admin | Тип контролю |
|---|---|:---:|:---:|:---:|---|
| Перегляд публічних сторінок | `/Home/*` | ✅ | ✅ | ✅ | Публічний доступ |
| Локальний вхід / реєстрація | `/Account/Login`, `/Account/Register` | ✅ | — | — | Публічний доступ |
| OAuth авторизація Spotify | `/Account/ChallengeSpotify` | ✅ | ✅ | ✅ | Публічний/Федеративний |
| Перегляд своєї бібліотеки | `/Library/Index` | ❌ (401) | ✅ | ✅ | RBAC + DAC (`OwnerSpotifyId`) |
| Імпорт лайкнутих пісень | `/Library/ImportTracks` | ❌ (401) | ✅ (свої) | ✅ | DAC (`OwnerSpotifyId`) |
| Підтягування жанрів | `/Library/ImportGenres` | ❌ (401) | ✅ (свої) | ✅ | DAC (`OwnerSpotifyId`) |
| Перегляд жанрових груп | `/Genres/*` | ❌ (401) | ✅ | ✅ | RBAC (`[Authorize]`) |
| Експорт плейлистів | `/Playlists/Export` | ❌ (401) | ✅ (свої) | ✅ | DAC (`OwnerSpotifyId`) |
| Перегляд деталей експорту | `/Playlists/Details/{id}` | ❌ (401) | ✅* | ✅ | DAC (тільки власник об'єкта або Admin) |
| Видалення експорту | `/Playlists/Delete/{id}` | ❌ (401) | ✅* | ✅ | DAC (тільки власник об'єкта або Admin) |
| Системна статистика | `/Admin/Index` | ❌ (401) | ❌ (403) | ✅ | RBAC (`[Authorize(Roles = "Admin")]`) |
| Перелік користувачів | `/Admin/Users` | ❌ (401) | ❌ (403) | ✅ | RBAC (`[Authorize(Roles = "Admin")]`) |
| Зміна ролі користувача | `/Admin/ChangeRole` | ❌ (401) | ❌ (403) | ✅ | RBAC (`[Authorize(Roles = "Admin")]`) |

\* *Операція дозволена тільки якщо `OwnerSpotifyId` збігається із поточним користувачем.*

## 4. Забезпечення принципу відмови за замовчуванням (Deny by default)
- Усі дії вимагають явної автентифікації (`[Authorize]`), крім виділених у `HomeController` та `AccountController`.
- Доступ до модулів адміністрування вимагає наявності клейму ролі `Admin`.
- Усі дії модифікації та читання конкретних записів перевіряють приналежність об'єкта перед виконанням будь-якої дії в базі даних.
- Спроби несанкціонованого доступу повертають HTTP 403 Forbidden (`ForbidResult`).
