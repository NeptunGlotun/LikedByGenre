using LikedByGenre.Models;

namespace LikedByGenre.Data;

public static class GenreSeed
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.GenreGroups.Any())
        {
            return;
        }

        var groups = new (string Name, string Slug, int Sort, string[] Tags)[]
        {
            ("Pop", "pop", 1, new[] { "pop", "dance pop", "indie pop", "synth-pop", "electropop", "art pop", "k-pop", "j-pop", "teen pop", "pop dance", "pop rap" }),
            ("Hip-Hop", "hip-hop", 2, new[] { "hip hop", "rap", "trap", "conscious hip hop", "southern hip hop", "gangster rap", "uk hip hop", "cloud rap", "drill" }),
            ("R&B", "rnb", 3, new[] { "r&b", "contemporary r&b", "soul", "neo soul", "quiet storm", "funk" }),
            ("Rock", "rock", 4, new[] { "rock", "classic rock", "alternative rock", "indie rock", "hard rock", "punk", "punk rock", "pop rock", "garage rock", "post-punk", "grunge" }),
            ("Metal", "metal", 5, new[] { "metal", "heavy metal", "death metal", "black metal", "thrash metal", "metalcore", "nu metal", "doom metal" }),
            ("Electronic", "electronic", 6, new[] { "electronic", "edm", "house", "techno", "trance", "dubstep", "drum and bass", "ambient", "electro", "idm", "deep house", "progressive house" }),
            ("Jazz", "jazz", 7, new[] { "jazz", "bebop", "cool jazz", "smooth jazz", "vocal jazz", "swing" }),
            ("Classical", "classical", 8, new[] { "classical", "orchestral", "opera", "baroque", "contemporary classical", "soundtrack", "film score" }),
            ("Folk", "folk", 9, new[] { "folk", "indie folk", "singer-songwriter", "americana", "country", "bluegrass", "acoustic" }),
            ("Latin", "latin", 10, new[] { "latin", "reggaeton", "salsa", "bachata", "latin pop", "cumbia", "brazilian" }),
            ("Other", "other", 100, Array.Empty<string>())
        };

        foreach (var group in groups)
        {
            var entity = new GenreGroup
            {
                Name = group.Name,
                Slug = group.Slug,
                SortOrder = group.Sort
            };
            db.GenreGroups.Add(entity);
            db.SaveChanges();

            foreach (var tag in group.Tags)
            {
                db.GenreMappings.Add(new GenreMapping
                {
                    SpotifyGenre = tag,
                    GenreGroupId = entity.Id
                });
            }
        }

        db.SaveChanges();
    }
}
