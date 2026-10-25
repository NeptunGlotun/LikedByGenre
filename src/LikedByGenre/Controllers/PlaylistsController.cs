using LikedByGenre.Data;
using LikedByGenre.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LikedByGenre.Controllers;

[Authorize]
public class PlaylistsController : Controller
{
    private readonly AppDbContext _db;
    private readonly PlaylistExportService _export;
    private readonly GenreClassificationService _classification;
    private readonly CurrentUser _user;

    public PlaylistsController(
        AppDbContext db,
        PlaylistExportService export,
        GenreClassificationService classification,
        CurrentUser user)
    {
        _db = db;
        _export = export;
        _classification = classification;
        _user = user;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Groups = await _classification.GetGroupSummariesAsync(cancellationToken);
        var exports = await _export.GetExportsAsync(cancellationToken);
        return View(exports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Export(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _export.ExportGroupAsync(slug, cancellationToken);
            TempData["Status"] = $"Створено плейлист «{created.Name}» ({created.TrackCount} треків).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Перегляд деталей експортованого плейлиста.
    /// Реалізація моделі DAC (Discretionary Access Control):
    /// доступ до ресурсу має виключно його власник (OwnerSpotifyId) або адміністратор (RBAC Superuser).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var export = await _db.PlaylistExports
            .Include(x => x.GenreGroup)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (export == null)
        {
            return NotFound();
        }

        var currentOwnerId = _user.SpotifyId;
        var isAdmin = User.IsInRole("Admin");

        // Безпосередня перевірка повноважень перед наданням доступу до ресурсу (DAC)
        if (export.OwnerSpotifyId != currentOwnerId && !isAdmin)
        {
            return Forbid();
        }

        return View(export);
    }

    /// <summary>
    /// Видалення запису про експорт плейлиста.
    /// Реалізація моделі DAC: перевірка прав володіння об'єктом перед виконанням захищеної модифікації.
    /// Спроба видалити чужий плейлист повертає HTTP 403 (ForbidResult).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var export = await _db.PlaylistExports
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (export == null)
        {
            return NotFound();
        }

        var currentOwnerId = _user.SpotifyId;
        var isAdmin = User.IsInRole("Admin");

        // DAC: суб'єкт повинен бути власником об'єкта або володіти роллю Admin
        if (export.OwnerSpotifyId != currentOwnerId && !isAdmin)
        {
            return Forbid();
        }

        _db.PlaylistExports.Remove(export);
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Status"] = $"Запис про експорт плейлиста «{export.Name}» видалено.";
        return RedirectToAction(nameof(Index));
    }
}
