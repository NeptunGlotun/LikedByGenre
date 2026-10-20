using LikedByGenre.Data;
using LikedByGenre.Models;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Services;

public class GenreClassificationService
{
    public const string OtherSlug = "other";

    private readonly AppDbContext _db;
    private readonly CurrentUser _user;

    public GenreClassificationService(AppDbContext db, CurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<IReadOnlyList<GenreGroupSummary>> GetGroupSummariesAsync(CancellationToken cancellationToken)
    {
        var groups = await _db.GenreGroups.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var classified = await ClassifyAsync(cancellationToken);
        return groups
            .Select(group => new GenreGroupSummary(
                group.Id,
                group.Name,
                group.Slug,
                classified.Count(x => x.Slug == group.Slug)))
            .ToList();
    }

    public async Task<GenreGroup?> FindGroupBySlugAsync(string slug, CancellationToken cancellationToken) =>
        await _db.GenreGroups.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Track>> GetTracksForGroupAsync(string slug, CancellationToken cancellationToken)
    {
        var classified = await ClassifyAsync(cancellationToken);
        return classified.Where(x => x.Slug == slug).Select(x => x.Track).ToList();
    }

    public async Task<IReadOnlyList<string>> GetUrisForGroupAsync(string slug, CancellationToken cancellationToken)
    {
        var tracks = await GetTracksForGroupAsync(slug, cancellationToken);
        return tracks.Select(x => x.Uri).ToList();
    }

    private async Task<List<ClassifiedTrack>> ClassifyAsync(CancellationToken cancellationToken)
    {
        var ownerId = _user.SpotifyId;
        var mappings = await _db.GenreMappings
            .AsNoTracking()
            .Include(x => x.GenreGroup)
            .ToListAsync(cancellationToken);
        var map = mappings.ToDictionary(
            x => x.SpotifyGenre,
            x => x.GenreGroup.Slug,
            StringComparer.OrdinalIgnoreCase);

        var tracks = await _db.Tracks
            .AsNoTracking()
            .Where(x => x.OwnerSpotifyId == ownerId)
            .Include(x => x.TrackArtists)
            .ThenInclude(x => x.Artist)
            .ThenInclude(x => x.Genres)
            .OrderBy(x => x.AddedAt)
            .ToListAsync(cancellationToken);

        return tracks.Select(track => new ClassifiedTrack(track, ResolveSlug(track, map))).ToList();
    }

    public static string ResolveSlug(Track track, IReadOnlyDictionary<string, string> map)
    {
        var mainArtist = track.TrackArtists.OrderBy(x => x.Order).FirstOrDefault()?.Artist;
        var rawGenre = mainArtist?.Genres.Select(x => x.Name).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(rawGenre) && map.TryGetValue(rawGenre, out var slug))
        {
            return slug;
        }

        return OtherSlug;
    }

    private sealed record ClassifiedTrack(Track Track, string Slug);
}

public record GenreGroupSummary(int Id, string Name, string Slug, int TrackCount);
