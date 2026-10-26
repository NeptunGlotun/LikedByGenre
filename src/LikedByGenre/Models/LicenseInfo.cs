using System.Text.Json.Serialization;

namespace LikedByGenre.Models;

/// <summary>
/// Модель цифрової ліцензії програмного забезпечення LikedByGenre (SSS).
/// Захищена від модифікації за допомогою асиметричного цифрового підпису RSA-PSS (3072 біт).
/// </summary>
public class LicenseInfo
{
    [JsonPropertyName("product_id")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("license_id")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("licensee")]
    public string Licensee { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Free";

    [JsonPropertyName("issued_at")]
    public DateTime IssuedAt { get; set; }

    [JsonPropertyName("valid_until")]
    public DateTime ValidUntil { get; set; }

    [JsonPropertyName("max_tracks")]
    public int MaxTracks { get; set; } = 100;

    [JsonPropertyName("max_playlists")]
    public int MaxPlaylists { get; set; } = 3;

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    public bool IsExpired => DateTime.UtcNow > ValidUntil;

    public bool HasFeature(string feature) =>
        Features.Any(f => string.Equals(f, feature, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Результат криптографічної перевірки цифрового підпису та параметрів ліцензії.
/// </summary>
public record LicenseValidationResult(
    bool IsValid,
    string Status,
    string Message,
    LicenseInfo? License = null);
