namespace LikedByGenre.Services;

/// <summary>
/// Інтерфейс служби симетричного криптографічного захисту даних.
/// Забезпечує автентифіковане шифрування (AEAD) конфіденційних даних
/// (сесійних токенів Spotify, персональних ключів тощо).
/// </summary>
public interface ISymmetricEncryptionService
{
    /// <summary>
    /// Шифрує відкритий рядок за допомогою AES-256-GCM.
    /// Повертає криптографічний пакет у форматі Base64: [12B Nonce][16B Tag][Ciphertext].
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Розшифровує криптографічний пакет із обов'язковою перевіркою автентифікаційного тегу.
    /// Викидає CryptographicException при будь-якій модифікації шифротексту.
    /// </summary>
    string Decrypt(string cipherPackageBase64);

    /// <summary>
    /// Безпечна спроба розшифрування без викидання винятків.
    /// </summary>
    bool TryDecrypt(string cipherPackageBase64, out string? plainText);
}
