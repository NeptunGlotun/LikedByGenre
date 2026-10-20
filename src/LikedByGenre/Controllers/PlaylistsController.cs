using LikedByGenre.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LikedByGenre.Controllers;

[Authorize]
public class PlaylistsController : Controller
{
    private readonly PlaylistExportService _export;
    private readonly GenreClassificationService _classification;

    public PlaylistsController(PlaylistExportService export, GenreClassificationService classification)
    {
        _export = export;
        _classification = classification;
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
}
