using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace LikedByGenre.Services;

public class CurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string SpotifyId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Користувач не автентифікований.");

    public string DisplayName =>
        _httpContextAccessor.HttpContext?.User.Identity?.Name ?? "Spotify user";

    public async Task<string> GetAccessTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Немає HTTP-контексту.");
        var token = await context.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Немає access token. Увійдіть знову через Spotify.");
        }

        return token;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Немає HTTP-контексту.");
        return await context.GetTokenAsync("refresh_token");
    }
}
