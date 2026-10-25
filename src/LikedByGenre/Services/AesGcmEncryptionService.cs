using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LikedByGenre.Services;

/// <summary>
/// Реалізація симетричного криптографічного захисту на базі AES-256 у режимі GCM (Galois/Counter Mode).
/// Забезпечує автентифіковане шифрування (AEAD): одночасну конфіденційність,
/// цілісність та автентичність даних із захистом від атак типу "known plaintext" та підробки шифротексту.
/// </summary>
public class AesGcmEncryptionService : ISymmetricEncryptionService
{
    private const int NonceSizeBytes = 12; // 96 біт - стандартний розмір Nonce для GCM
    private const int TagSizeBytes = 16;   // 128 біт - максимальний розмір автентифікаційної мітки
    private readonly byte[] _key;

    public AesGcmEncryptionService(IConfiguration configuration)
    {
        // Отримуємо 256-бітний ключ шифрування з конфігурації/UserSecrets
        var configuredKey = configuration["Security:EncryptionKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey) && configuredKey.Length == 64)
        {
            _key = Convert.FromHexString(configuredKey);
        }
        else
        {
            // Якщо ключ не задано явно, детерміновано формуємо 256-бітний ключ
            // на базі конфігураційного хешу або секрету Spotify
            var secretSeed = configuration["Spotify:ClientSecret"] ?? "LikedByGenre_Master_Encryption_Seed_2026_Key!";
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(secretSeed));
        }
    }

    /// <summary>
    /// Конструктор з прямим заданням ключа (для тестів).
    /// </summary>
    public AesGcmEncryptionService(byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new ArgumentException("Ключ повинен мати розмір 256 біт (32 байти).", nameof(key));
        }
        _key = (byte[])key.Clone();
    }

    public string Encrypt(string plainText)
    {
        if (plainText is null)
        {
            throw new ArgumentNullException(nameof(plainText));
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce); // Унікальний криптографічний Nonce для кожної операції

        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSizeBytes];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Пакет: [Nonce (12B)] + [Tag (16B)] + [Ciphertext (NB)]
        byte[] package = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, package, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, package, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(cipherBytes, 0, package, NonceSizeBytes + TagSizeBytes, cipherBytes.Length);

        return Convert.ToBase64String(package);
    }

    public string Decrypt(string cipherPackageBase64)
    {
        if (string.IsNullOrWhiteSpace(cipherPackageBase64))
        {
            throw new ArgumentException("Шифротекст не може бути порожнім.", nameof(cipherPackageBase64));
        }

        byte[] package = Convert.FromBase64String(cipherPackageBase64);
        if (package.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Неприпустимий розмір криптографічного пакета.");
        }

        byte[] nonce = new byte[NonceSizeBytes];
        byte[] tag = new byte[TagSizeBytes];
        int cipherLen = package.Length - NonceSizeBytes - TagSizeBytes;
        byte[] cipherBytes = new byte[cipherLen];

        Buffer.BlockCopy(package, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(package, NonceSizeBytes, tag, 0, TagSizeBytes);
        Buffer.BlockCopy(package, NonceSizeBytes + TagSizeBytes, cipherBytes, 0, cipherLen);

        byte[] plainBytes = new byte[cipherLen];

        using (var aesGcm = new AesGcm(_key, TagSizeBytes))
        {
            // Decrypt автоматично перевіряє автентифікаційну мітку tag.
            // При невідповідності або модифікації хоча б 1 біта генерується CryptographicException.
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    public bool TryDecrypt(string cipherPackageBase64, out string? plainText)
    {
        try
        {
            plainText = Decrypt(cipherPackageBase64);
            return true;
        }
        catch (Exception)
        {
            plainText = null;
            return false;
        }
    }
}
