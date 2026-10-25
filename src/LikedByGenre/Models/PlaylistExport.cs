namespace LikedByGenre.Models;

public class PlaylistExport
{
    public int Id { get; set; }
    public required string OwnerSpotifyId { get; set; }
    public int GenreGroupId { get; set; }
    public GenreGroup GenreGroup { get; set; } = null!;
    public required string SpotifyPlaylistId { get; set; }
    public required string Name { get; set; }
    public int TrackCount { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// HMAC-SHA256 підпис цілісності та автентичності запису експорту.
    /// Захищає від несанкціонованої модифікації атрибутів експорту в БД.
    /// </summary>
    public string? IntegritySignature { get; set; }
}
