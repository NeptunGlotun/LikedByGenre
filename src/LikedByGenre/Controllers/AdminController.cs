using LikedByGenre.Data;
using LikedByGenre.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Controllers;

/// <summary>
/// Контролер адміністрування.
/// Реалізація рольової моделі керування доступом (RBAC — Role-Based Access Control):
/// доступ до будь-якої дії контролера дозволено виключно суб'єктам із роллю "Admin".
/// Будь-який запит від звичайного користувача ("User") або гостя блокується
/// на рівні обробки авторизаційного конвеєра ASP.NET Core (HTTP 403 Forbidden).
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Головна панель адміністратора: статистика системи.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.TotalUsers = await _db.Users.CountAsync(cancellationToken);
        ViewBag.TotalTracks = await _db.Tracks.CountAsync(cancellationToken);
        ViewBag.TotalArtists = await _db.Artists.CountAsync(cancellationToken);
        ViewBag.TotalExports = await _db.PlaylistExports.CountAsync(cancellationToken);
        ViewBag.TotalGenreGroups = await _db.GenreGroups.CountAsync(cancellationToken);

        var recentUsers = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        return View(recentUsers);
    }

    /// <summary>
    /// Перелік усіх користувачів системи з можливістю керування ролями.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

        return View(users);
    }

    /// <summary>
    /// Зміна ролі користувача (RBAC).
    /// Захищена операція: доступна тільки Admin, із перевіркою неприпустимості видалення останнього Admin.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int id, string newRole, CancellationToken cancellationToken)
    {
        if (newRole != "Admin" && newRole != "User")
        {
            TempData["Error"] = "Неприпустиме значення ролі.";
            return RedirectToAction(nameof(Users));
        }

        var targetUser = await _db.Users.FindAsync(new object[] { id }, cancellationToken);
        if (targetUser == null)
        {
            TempData["Error"] = "Користувача не знайдено.";
            return RedirectToAction(nameof(Users));
        }

        // Захист від втрати доступу: заборонено позбавляти прав єдиного адміністратора
        if (targetUser.Role == "Admin" && newRole == "User")
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == "Admin", cancellationToken);
            if (adminCount <= 1)
            {
                TempData["Error"] = "Неможливо понизити роль єдиного адміністратора в системі.";
                return RedirectToAction(nameof(Users));
            }
        }

        targetUser.Role = newRole;
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Status"] = $"Роль користувача «{targetUser.Username}» успішно змінено на «{newRole}».";
        return RedirectToAction(nameof(Users));
    }
}
