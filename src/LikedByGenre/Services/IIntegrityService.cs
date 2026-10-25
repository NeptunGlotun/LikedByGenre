namespace LikedByGenre.Services;

/// <summary>
/// Інтерфейс служби контролю цілісності та автентичності даних.
/// Забезпечує обчислення криптографічних хеш-значень (SHA-256),
/// генерацію та перевірку кодів автентичності повідомлень (HMAC-SHA256).
/// </summary>
public interface IIntegrityService
{
    /// <summary>
    /// Обчислює криптографічний хеш SHA-256 від вхідного рядка (контроль незмінності).
    /// </summary>
    string ComputeHash(string data);

    /// <summary>
    /// Генерує HMAC-SHA256 підпис для повідомлення на основі секретного системного ключа.
    /// </summary>
    string ComputeHmac(string data);

    /// <summary>
    /// Безпечно перевіряє HMAC-підпис за фіксований час (constant-time, захист від timing-атак).
    /// </summary>
    bool VerifyHmac(string data, string expectedHmacHex);

    /// <summary>
    /// Формує криптографічний HMAC-підпис для сутності PlaylistExport.
    /// </summary>
    string SignExport(string ownerSpotifyId, string spotifyPlaylistId, int trackCount, DateTime createdAt);

    /// <summary>
    /// Перевіряє цілісність та автентичність сутності PlaylistExport.
    /// </summary>
    bool VerifyExport(string ownerSpotifyId, string spotifyPlaylistId, int trackCount, DateTime createdAt, string? signatureHex);
}
