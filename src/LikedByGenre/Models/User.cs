namespace LikedByGenre.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? SpotifyId { get; set; }

    /// <summary>
    /// Роль користувача для рольової моделі керування доступом (RBAC).
    /// Можливі значення: "Admin" або "User".
    /// </summary>
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
