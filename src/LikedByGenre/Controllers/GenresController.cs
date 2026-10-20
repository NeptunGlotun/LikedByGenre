using LikedByGenre.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LikedByGenre.Controllers;

[Authorize]
public class GenresController : Controller
{
    private readonly GenreClassificationService _classification;

    public GenresController(GenreClassificationService classification)
    {
        _classification = classification;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var groups = await _classification.GetGroupSummariesAsync(cancellationToken);
        return View(groups);
    }

    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var group = await _classification.FindGroupBySlugAsync(slug, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        var tracks = await _classification.GetTracksForGroupAsync(slug, cancellationToken);
        ViewBag.Group = group;
        return View(tracks);
    }
}
