namespace LikedByGenre.Models;

public class Track
{
    public int Id { get; set; }
    public required string OwnerSpotifyId { get; set; }
    public required string SpotifyId { get; set; }
    public required string Uri { get; set; }
    public required string Name { get; set; }
    public string? AlbumName { get; set; }
    public int DurationMs { get; set; }
    public DateTime AddedAt { get; set; }

    public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
}
