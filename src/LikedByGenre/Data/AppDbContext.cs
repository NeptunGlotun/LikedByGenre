using LikedByGenre.Models;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<TrackArtist> TrackArtists => Set<TrackArtist>();
    public DbSet<ArtistGenre> ArtistGenres => Set<ArtistGenre>();
    public DbSet<GenreGroup> GenreGroups => Set<GenreGroup>();
    public DbSet<GenreMapping> GenreMappings => Set<GenreMapping>();
    public DbSet<PlaylistExport> PlaylistExports => Set<PlaylistExport>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasIndex(x => new { x.OwnerSpotifyId, x.SpotifyId }).IsUnique();
            entity.Property(x => x.OwnerSpotifyId).HasMaxLength(64);
            entity.Property(x => x.SpotifyId).HasMaxLength(64);
            entity.Property(x => x.Uri).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(512);
            entity.Property(x => x.AlbumName).HasMaxLength(512);
        });

        modelBuilder.Entity<Artist>(entity =>
        {
            entity.HasIndex(x => x.SpotifyId).IsUnique();
            entity.Property(x => x.SpotifyId).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(512);
        });

        modelBuilder.Entity<TrackArtist>(entity =>
        {
            entity.HasKey(x => new { x.TrackId, x.ArtistId });
            entity.HasOne(x => x.Track).WithMany(x => x.TrackArtists).HasForeignKey(x => x.TrackId);
            entity.HasOne(x => x.Artist).WithMany(x => x.TrackArtists).HasForeignKey(x => x.ArtistId);
        });

        modelBuilder.Entity<ArtistGenre>(entity =>
        {
            entity.HasKey(x => new { x.ArtistId, x.Name });
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.HasOne(x => x.Artist).WithMany(x => x.Genres).HasForeignKey(x => x.ArtistId);
        });

        modelBuilder.Entity<GenreGroup>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(64);
        });

        modelBuilder.Entity<GenreMapping>(entity =>
        {
            entity.HasIndex(x => x.SpotifyGenre).IsUnique();
            entity.Property(x => x.SpotifyGenre).HasMaxLength(256);
            entity.HasOne(x => x.GenreGroup).WithMany(x => x.Mappings).HasForeignKey(x => x.GenreGroupId);
        });

        modelBuilder.Entity<PlaylistExport>(entity =>
        {
            entity.Property(x => x.OwnerSpotifyId).HasMaxLength(64);
            entity.Property(x => x.SpotifyPlaylistId).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(64);
            entity.Property(x => x.PasswordHash).HasMaxLength(128);
            entity.Property(x => x.SpotifyId).HasMaxLength(64);
            entity.Property(x => x.Role).HasMaxLength(32).HasDefaultValue("User");
        });
    }
}
