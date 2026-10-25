using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LikedByGenre.Services;

/// <summary>
/// Реалізація механізмів криптографічного контролю цілісності та автентичності.
/// Використовує алгоритми SHA-256 (цілісність) та HMAC-SHA256 (цілісність + автентичність),
/// а також порівняння за константний час (CryptographicOperations.FixedTimeEquals).
/// </summary>
public class HmacIntegrityService : IIntegrityService
{
    private readonly byte[] _key;

    public HmacIntegrityService(IConfiguration configuration)
    {
        var configuredKey = configuration["Security:IntegrityKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey) && configuredKey.Length == 64)
        {
            _key = Convert.FromHexString(configuredKey);
        }
        else
        {
            // Формуємо окремий 256-бітний ключ цілісності через доменне розділення (KDF)
            var seed = configuration["Spotify:ClientSecret"] ?? "LikedByGenre_Integrity_Authenticity_Key_2026!";
            _key = HMACSHA256.HashData(Encoding.UTF8.GetBytes("IntegrityKey_Domain_Separation_2026"), Encoding.UTF8.GetBytes(seed));
        }
    }

    /// <summary>
    /// Конструктор для прямого задання ключа (для тестів).
    /// </summary>
    public HmacIntegrityService(byte[] key)
    {
        if (key == null || key.Length != 32)
        {
            throw new ArgumentException("Ключ HMAC повинен мати розмір 256 біт (32 байти).", nameof(key));
        }
        _key = (byte[])key.Clone();
    }

    public string ComputeHash(string data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string ComputeHmac(string data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(_key);
        byte[] mac = hmac.ComputeHash(bytes);
        return Convert.ToHexString(mac).ToLowerInvariant();
    }

    public bool VerifyHmac(string data, string expectedHmacHex)
    {
        if (data == null || string.IsNullOrWhiteSpace(expectedHmacHex))
        {
            return false;
        }

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHmacHex);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualBytes;
        using (var hmac = new HMACSHA256(_key))
        {
            actualBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        // Порівняння за фіксований час для захисту від Side-Channel Timing Attacks
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    public string SignExport(string ownerSpotifyId, string spotifyPlaylistId, int trackCount, DateTime createdAt)
    {
        string canonicalData = $"{ownerSpotifyId}|{spotifyPlaylistId}|{trackCount}|{createdAt.ToUniversalTime():O}";
        return ComputeHmac(canonicalData);
    }

    public bool VerifyExport(string ownerSpotifyId, string spotifyPlaylistId, int trackCount, DateTime createdAt, string? signatureHex)
    {
        if (string.IsNullOrWhiteSpace(signatureHex))
        {
            return false;
        }

        string canonicalData = $"{ownerSpotifyId}|{spotifyPlaylistId}|{trackCount}|{createdAt.ToUniversalTime():O}";
        return VerifyHmac(canonicalData, signatureHex);
    }
}
