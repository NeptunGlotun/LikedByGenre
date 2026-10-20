# ====== Classification BUSINESS LOGIC ======

> Специфікація концептуального змісту модуля `Classification` відповідно до шаблону ЛР№2. Модуль забезпечує мапінг сирих жанрів Spotify на визначені узагальнені групи жанрів.

## Purpose

`Classification` володіє:
- каталогом цільових груп жанрів (`GenreGroup`);
- словником відповідності (мапінгом) сирих текстових тегів Spotify до груп жанрів (`GenreMapping`);
- алгоритмом детермінованого віднесення трека користувача до однієї з жанрових груп на основі жанрів його артистів;
- розрахунком зведеної статистики кількості треків у кожній групі для поточного користувача;
- вибіркою треків та Spotify URI для певної групи жанрів.

**NOT here:**
- автентифікація та сесія користувача — `Identity`;
- отримання треків та жанрів артистів зі Spotify API — `Library`;
- створення та експорт плейлистів у Spotify — `Playlists`.

## Entities

| Entity | Basic Fields | Description | Invariants |
|---|---|---|---|
| `GenreGroup` *(Aggregate Root)* | Id, Name, Slug, SortOrder | Узагальнена категорія жанру (наприклад, Pop, Rock, Electronic). | - `Slug` унікальний та незмінний після створення.<br>- `Name` обов'язковий, довжина до 64 символів.<br>- Група `Other` (`slug: other`, `SortOrder: 100`) є обов'язковою системною категорією для треків без визначеного жанру. |
| `GenreMapping` *(Internal Entity — owned by `GenreGroup`)* | Id, SpotifyGenre, GenreGroupId | Правило зіставлення одного сирого тегу Spotify із цільовою групою жанру. | - `SpotifyGenre` унікальний у межах системи та зберігається у нижньому регістрі (`lowercase`).<br>- `GenreGroupId` є валідним зовнішнім ключем до існуючої `GenreGroup`.<br>- Порожні або whitespace-значення не допускаються. |

## Ключовий флоу: Класифікація треків

1. Отримати `OwnerSpotifyId` поточного автентифікованого користувача.
2. Завантажити повний мапінг `GenreMappings` з прив'язкою до `GenreGroup.Slug` у швидкий словник пам'яті (`Dictionary<string, string>`).
3. Вибрати всі збережені треки цього користувача разом із їхніми артистами та сирими тегами `Artist.Genres` (впорядковано за `Order` артиста).
4. Для кожного треку викликати детерміновану функцію `ResolveSlug(track, map)`:
   - Знайти основного артиста треку (`Order == 0`).
   - Взяти перший доступний непорожній сирий жанровий тег цього артиста.
   - Якщо знайдено відповідність у словнику мапінгу — призначити відповідний `GenreGroup.Slug`.
   - Якщо теги відсутні, або жоден не знайдений у словнику — призначити системний slug `other`.
5. Повернути список класифікованих треків або згруповану статистику `GenreGroupSummary`.

**Boundary:** Модуль класифікації оперує виключно локальними даними з бази даних і не здійснює мережевих запитів до Spotify.

## Domain Services

Кожен метод інкапсулює одну операцію над класифікацією треків:

| Domain Service / Method | Operation |
|---|---|
| `GenreClassificationService.GetGroupSummariesAsync` | Формує список усіх жанрових груп із кількістю класифікованих треків поточного користувача для кожної групи. |
| `GenreClassificationService.FindGroupBySlugAsync` | Знаходить сутність групи за її унікальним `slug`. |
| `GenreClassificationService.GetTracksForGroupAsync` | Повертає повний список об'єктів `Track`, що належать користувачу і класифіковані до заданого `slug`. |
| `GenreClassificationService.GetUrisForGroupAsync` | Повертає список рядкових `Uri` (Spotify Track URI) для треків заданої групи (використовується для експорту). |
| `GenreClassificationService.ResolveSlug` *(чиста функція)* | Детерміновано співставляє трек і словник мапінгу до відповідного `slug`. |

## Application Commands & Queries

**Commands (`AC`):**

| Area | Commands |
|---|---|
| Сід / Наповнення | `GenreSeed.EnsureSeeded` (ініціалізація груп та базових мапінгів при першому запуску) |

**Queries (`AQ`):**

| Query | Purpose |
|---|---|
| `GetGroupSummariesAQ` | Отримання переліку категорій із лічильниками пісень для сторінки `/Genres`. |
| `GetGroupDetailsAQ` | Отримання списку треків конкретної групи для сторінки `/Genres/Details/{slug}`. |
| `GetGroupUrisAQ` | Отримання Spotify URI треків групи для підготовки експорту в плейлист. |

## Infrastructure

### Models

- `GenreGroups`, `GenreMappings` — таблиці в SQL Server, налаштовані через `AppDbContext`.
- Ініціалізація початкового набору груп (Pop, Hip-Hop, R&B, Rock, Metal, Electronic, Jazz, Classical, Folk, Latin, Other) здійснюється через статичний сід `GenreSeed.EnsureSeeded()`.
- Запити на класифікацію виконуються з прапорцем `AsNoTracking()`, що запобігає зайвому навантаженню на пам'ять при обробці великих списків пісень (2000+ треків).
