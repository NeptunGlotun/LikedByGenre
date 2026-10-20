namespace LikedByGenre.Models;

public class GenreGroup
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public int SortOrder { get; set; }

    public ICollection<GenreMapping> Mappings { get; set; } = new List<GenreMapping>();
    public ICollection<PlaylistExport> PlaylistExports { get; set; } = new List<PlaylistExport>();
}
