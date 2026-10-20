namespace LikedByGenre.Models;

public class Artist
{
    public int Id { get; set; }
    public required string SpotifyId { get; set; }
    public required string Name { get; set; }
    public DateTime? GenreFetchedAt { get; set; }
    public bool GenreFieldMissing { get; set; }

    public ICollection<ArtistGenre> Genres { get; set; } = new List<ArtistGenre>();
    public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
}
