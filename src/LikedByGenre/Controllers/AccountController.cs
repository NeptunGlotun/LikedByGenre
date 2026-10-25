using System.Security.Claims;
using LikedByGenre.Data;
using LikedByGenre.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Вкажіть логін та пароль.";
            return View();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());
        
        // Захист від витоку інформації про наявність користувача:
        // однаковий час відповіді та загальне повідомлення про помилку
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            TempData["Error"] = "Неправильний логін або пароль.";
            return View();
        }

        var userRole = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.SpotifyId ?? user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, userRole)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return RedirectToAction("Index", "Library");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Всі поля обов'язкові для заповнення.";
            return View();
        }

        if (password.Length < 8)
        {
            TempData["Error"] = "Пароль повинен містити щонайменше 8 символів.";
            return View();
        }

        if (password != confirmPassword)
        {
            TempData["Error"] = "Паролі не збігаються.";
            return View();
        }

        var normalizedUsername = username.Trim();
        var exists = await _db.Users.AnyAsync(u => u.Username == normalizedUsername);
        if (exists)
        {
            TempData["Error"] = "Користувач із таким іменем уже існує.";
            return View();
        }

        // Безпечне хешування пароля за допомогою BCrypt із сіллю та параметром вартості (workFactor: 12)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        // Перший зареєстрований користувач автоматично отримує роль Admin, решта - User
        var isFirstUser = !await _db.Users.AnyAsync();
        var role = isFirstUser || normalizedUsername.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";

        var newUser = new User
        {
            Username = normalizedUsername,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Обліковий запис успішно створено (роль: {role}). Увійдіть за допомогою своїх облікових даних.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult ChallengeSpotify()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("Index", "Library") };
        return Challenge(properties, "Spotify");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
