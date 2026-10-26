using LikedByGenre.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LikedByGenre.Controllers;

[Authorize]
public class LicenseController : Controller
{
    private readonly ILicenseValidationService _licenseService;

    public LicenseController(ILicenseValidationService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var status = _licenseService.GetCurrentLicenseStatus();
        ViewBag.PublicKey = _licenseService.GetPublicKeyPem();
        return View(status);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyCustom(string licenseJson, string signatureBase64)
    {
        if (string.IsNullOrWhiteSpace(licenseJson) || string.IsNullOrWhiteSpace(signatureBase64))
        {
            TempData["Error"] = "Вкажіть вміст ліцензії та підпис у форматі Base64.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64.Trim());
            var result = _licenseService.ValidateLicense(licenseJson, signatureBytes);

            if (result.IsValid)
            {
                TempData["Status"] = $"[УСПІХ] {result.Message}";
            }
            else
            {
                TempData["Error"] = $"[ВІДХИЛЕНО] {result.Message}";
            }
        }
        catch (FormatException)
        {
            TempData["Error"] = "Помилка декодування Base64 для цифрового підпису.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Помилка валідації: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
