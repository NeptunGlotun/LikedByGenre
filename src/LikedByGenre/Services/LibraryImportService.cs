using LikedByGenre.Data;
using LikedByGenre.Models;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Services;

public class LibraryImportService
{
    public const int ArtistBatchSize = 40;

    private readonly AppDbContext _db;
    private readonly SpotifyApiClient _spotify;
    private readonly CurrentUser _user;

    public LibraryImportService(AppDbContext db, SpotifyApiClient spotify, CurrentUser user)
    {
        _db = db;
        _spotify = spotify;
        _user = user;
    }

    public async Task<LibraryStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var ownerId = _user.SpotifyId;
        var trackCount = await _db.Tracks.CountAsync(x => x.OwnerSpotifyId == ownerId, cancellationToken);
        var artistIds = await _db.TrackArtists
            .Where(x => x.Track.OwnerSpotifyId == ownerId)
            .Select(x => x.ArtistId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pendingArtists = await _db.Artists.CountAsync(
            x => artistIds.Contains(x.Id) && x.GenreFetchedAt == null,
            cancellationToken);

        return new LibraryStatus(trackCount, artistIds.Count, pendingArtists);
    }

    public async Task<int> ImportLikedTracksAsync(CancellationToken cancellationToken)
    {
        var ownerId = _user.SpotifyId;
        var accessToken = await _user.GetAccessTokenAsync();
        var offset = 0;
        var imported = 0;
        var existing = await _db.Tracks
            .Where(x => x.OwnerSpotifyId == ownerId)
            .Select(x => x.SpotifyId)
            .ToListAsync(cancellationToken);
        var knownTrackIds = existing.ToHashSet(StringComparer.Ordinal);
        var artists = await _db.Artists.ToDictionaryAsync(x => x.SpotifyId, cancellationToken);

        while (true)
        {
            var page = await _spotify.GetSavedTracksAsync(accessToken, offset, cancellationToken);
            if (page.Items.Count == 0)
            {
                break;
            }

            foreach (var item in page.Items)
            {
                var dto = item.Track;
                if (dto?.Id is null || dto.Uri is null || dto.Name is null)
                {
                    continue;
                }

                if (!knownTrackIds.Add(dto.Id))
                {
                    continue;
                }

                var track = new Track
                {
                    OwnerSpotifyId = ownerId,
                    SpotifyId = dto.Id,
                    Uri = dto.Uri,
                    Name = dto.Name,
                    AlbumName = dto.Album?.Name,
                    DurationMs = dto.DurationMs,
                    AddedAt = item.AddedAt
                };

                var order = 0;
                foreach (var artistRef in dto.Artists)
                {
                    if (string.IsNullOrWhiteSpace(artistRef.Id) || string.IsNullOrWhiteSpace(artistRef.Name))
                    {
                        continue;
                    }

                    if (!artists.TryGetValue(artistRef.Id, out var artist))
                    {
                        artist = new Artist { SpotifyId = artistRef.Id, Name = artistRef.Name };
                        _db.Artists.Add(artist);
                        artists[artistRef.Id] = artist;
                    }

                    track.TrackArtists.Add(new TrackArtist
                    {
                        Artist = artist,
                        Order = order++
                    });
                }

                _db.Tracks.Add(track);
                imported++;
            }

            await _db.SaveChangesAsync(cancellationToken);
            offset += 50;
            if (offset >= page.Total)
            {
                break;
            }
        }

        return imported;
    }

    public async Task<int> FetchArtistGenresAsync(CancellationToken cancellationToken)
    {
        var ownerId = _user.SpotifyId;
        var accessToken = await _user.GetAccessTokenAsync();
        var pending = await _db.Artists
            .Where(artist => artist.GenreFetchedAt == null && artist.TrackArtists.Any(x => x.Track.OwnerSpotifyId == ownerId))
            .OrderBy(x => x.Id)
            .Take(ArtistBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var artist in pending)
        {
            var dto = await _spotify.GetArtistAsync(accessToken, artist.SpotifyId, cancellationToken);
            artist.GenreFetchedAt = DateTime.UtcNow;
            if (dto?.Genres is null)
            {
                artist.GenreFieldMissing = true;
            }
            else
            {
                foreach (var genre in dto.Genres.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    _db.ArtistGenres.Add(new ArtistGenre
                    {
                        ArtistId = artist.Id,
                        Name = genre.Trim().ToLowerInvariant()
                    });
                }
            }

            await Task.Delay(80, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }
}

public record LibraryStatus(int TrackCount, int ArtistCount, int PendingArtistGenres);
