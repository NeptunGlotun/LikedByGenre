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
    private readonly IIntegrityService _integrity;

    public PlaylistExportService(
        AppDbContext db,
        SpotifyApiClient spotify,
        CurrentUser user,
        GenreClassificationService classification,
        IIntegrityService integrity)
    {
        _db = db;
        _spotify = spotify;
        _user = user;
        _classification = classification;
        _integrity = integrity;
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

        var now = DateTime.UtcNow;
        var ownerSpotifyId = _user.SpotifyId;
        var trackCount = uris.Count;

        // Формування криптографічного HMAC-підпису для забезпечення цілісності та автентичності
        var integritySignature = _integrity.SignExport(ownerSpotifyId, playlistId, trackCount, now);

        var export = new PlaylistExport
        {
            OwnerSpotifyId = ownerSpotifyId,
            GenreGroupId = group.Id,
            SpotifyPlaylistId = playlistId,
            Name = name,
            TrackCount = trackCount,
            CreatedAt = now,
            IntegritySignature = integritySignature
        };
        _db.PlaylistExports.Add(export);
        await _db.SaveChangesAsync(cancellationToken);
        return export;
    }

    /// <summary>
    /// Перевіряє цілісність та автентичність запису експорту плейлиста.
    /// Повертає true, якщо запис не було підроблено або модифіковано сторонніми особами.
    /// </summary>
    public bool VerifyExportIntegrity(PlaylistExport export)
    {
        return _integrity.VerifyExport(
            export.OwnerSpotifyId,
            export.SpotifyPlaylistId,
            export.TrackCount,
            export.CreatedAt,
            export.IntegritySignature);
    }
}
