# Contexts registry

Модулі відповідають коду в `src/LikedByGenre`. Окремих збірок / bounded-context проєктів немає.

| Контекст | Код | Специфікація | Відповідальність |
|---|---|---|---|
| Identity | `Controllers/AccountController.cs`, cookie + OAuth у `Program.cs`, `Services/CurrentUser.cs` | [business_logic.md](Identity/business_logic.md) | Вхід через Spotify, сесія, ідентифікатор користувача |
| Library | `Services/LibraryImportService.cs`, `Controllers/LibraryController.cs` | [business_logic.md](Library/business_logic.md) | Імпорт Liked Songs і жанрів артистів |
| Classification | `Services/GenreClassificationService.cs`, `Data/GenreSeed.cs`, `Controllers/GenresController.cs` | [business_logic.md](Classification/business_logic.md) | Віднесення трека до широкої групи жанру |
| Playlists | `Services/PlaylistExportService.cs`, `Controllers/PlaylistsController.cs` | [business_logic.md](Playlists/business_logic.md) | Створення плейлистів у Spotify |
| Infrastructure | `Data/AppDbContext.cs`, `Services/SpotifyApiClient.cs` | — | SQL Server і виклики Spotify Web API |

Немає білінгу, каталогу товарів, черг, кешу, вебхуків.

