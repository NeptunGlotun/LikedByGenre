namespace LikedByGenre.Models;

public class ArtistGenre
{
    public int ArtistId { get; set; }
    public Artist Artist { get; set; } = null!;

    public required string Name { get; set; }
}
