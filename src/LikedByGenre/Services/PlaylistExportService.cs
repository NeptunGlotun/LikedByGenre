using LikedByGenre.Data;
using LikedByGenre.Models;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Services;

public class PlaylistExportService
{
    private readonly AppDbContext _db;
    private readonly SpotifyApiClient _spotify;
    private readonly CurrentUser _user;
    private readonly GenreClassificationService _classification;

    public PlaylistExportService(
        AppDbContext db,
        SpotifyApiClient spotify,
        CurrentUser user,
        GenreClassificationService classification)
    {
        _db = db;
        _spotify = spotify;
        _user = user;
        _classification = classification;
    }

    public async Task<IReadOnlyList<PlaylistExport>> GetExportsAsync(CancellationToken cancellationToken)
    {
        var ownerId = _user.SpotifyId;
        return await _db.PlaylistExports
            .AsNoTracking()
            .Include(x => x.GenreGroup)
            .Where(x => x.OwnerSpotifyId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlaylistExport> ExportGroupAsync(string slug, CancellationToken cancellationToken)
    {
        var group = await _classification.FindGroupBySlugAsync(slug, cancellationToken)
                    ?? throw new InvalidOperationException("Невідома група жанру.");
        var uris = await _classification.GetUrisForGroupAsync(slug, cancellationToken);
        if (uris.Count == 0)
        {
            throw new InvalidOperationException($"У групі «{group.Name}» немає пісень для експорту.");
        }

        var accessToken = await _user.GetAccessTokenAsync();
        var name = $"Liked · {group.Name}";
        var playlistId = await _spotify.CreatePlaylistAsync(
            accessToken,
            name,
            "Плейлист зібрано з лайкнутих пісень додатком LikedByGenre.",
            cancellationToken);
        await _spotify.AddPlaylistItemsAsync(accessToken, playlistId, uris, cancellationToken);

        var export = new PlaylistExport
        {
            OwnerSpotifyId = _user.SpotifyId,
            GenreGroupId = group.Id,
            SpotifyPlaylistId = playlistId,
            Name = name,
            TrackCount = uris.Count,
            CreatedAt = DateTime.UtcNow
        };
        _db.PlaylistExports.Add(export);
        await _db.SaveChangesAsync(cancellationToken);
        return export;
    }
}
