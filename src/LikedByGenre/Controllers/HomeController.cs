using Microsoft.AspNetCore.Mvc;

namespace LikedByGenre.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
