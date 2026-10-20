namespace LikedByGenre.Models;

public class GenreMapping
{
    public int Id { get; set; }
    public required string SpotifyGenre { get; set; }
    public int GenreGroupId { get; set; }
    public GenreGroup GenreGroup { get; set; } = null!;
}
