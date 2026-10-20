using LikedByGenre.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LikedByGenre.Controllers;

[Authorize]
public class LibraryController : Controller
{
    private readonly LibraryImportService _import;

    public LibraryController(LibraryImportService import)
    {
        _import = import;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var status = await _import.GetStatusAsync(cancellationToken);
        return View(status);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportTracks(CancellationToken cancellationToken)
    {
        try
        {
            var imported = await _import.ImportLikedTracksAsync(cancellationToken);
            TempData["Status"] = imported == 0
                ? "Нових лайкнутих пісень не знайдено. Бібліотека вже синхронізована."
                : $"Додано {imported} пісень з Liked Songs.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportGenres(CancellationToken cancellationToken)
    {
        try
        {
            var fetched = await _import.FetchArtistGenresAsync(cancellationToken);
            TempData["Status"] = fetched == 0
                ? "Жанри всіх артистів уже завантажено."
                : $"Отримано жанри для {fetched} артистів. Якщо залишились необроблені, натисніть ще раз.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
