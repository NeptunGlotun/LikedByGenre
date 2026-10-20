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
}
