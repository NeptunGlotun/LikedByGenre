using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LikedByGenre.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace LikedByGenre.Services;

/// <summary>
/// Реалізація контролю ліцензійних умов та протидії несанкціонованому копіюванню
/// на базі асиметричного цифрового підпису RSA-PSS (3072 біт) із SHA-256.
/// Відповідає стандарту PKCS#1 v2.2 / RFC 8017.
/// </summary>
public class RsaPssLicenseValidationService : ILicenseValidationService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<RsaPssLicenseValidationService> _logger;
    private readonly string _publicKeyPem;
    private readonly RSA _rsaPublic;

    public const string ExpectedProductId = "LikedByGenre-SSS";

    public RsaPssLicenseValidationService(IWebHostEnvironment env, ILogger<RsaPssLicenseValidationService> logger)
    {
        _env = env;
        _logger = logger;

        string publicKeyPath = Path.Combine(_env.ContentRootPath, "Licenses", "public.pem");
        if (File.Exists(publicKeyPath))
        {
            _publicKeyPem = File.ReadAllText(publicKeyPath);
        }
        else
        {
            // Вбудований резервний відкритий ключ RSA 3072 біт
            _publicKeyPem = 
@"-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAuTHjQtrWgpNB73NArZrv
h+DL9GTlgJxP328z9O7WprWPo7PkTcxZ1kaFfaXiaKUjdUVnkU3auZy21JxgJv7D
1ye5iaamJvTNYG4Ew90MlH1ba3ZtKKfsbf5F/Wd3fG9wEPCZYfUSszQA4UvNr6/e
Pvls7VLzygbup5N9RJyPkXdzpZ9oiE45lfxr4yGCxU5fvYv2vUFGMw/f5srzFZPo
XmD8s2WX0q1fGyw5ZAmfJZ9i0+VTPjj3y0adwLjBq7QReMsXZoMq+Xi6YWUeD7zW
oCzierUzxinLxtqErinDorMgDvpvN13vxzgi53T9kKaN9e5ZiSaOawe0+RGeGmod
ME9bcG768BGeahCmKV6P2iUhsi/QX63G5gHj4med+FKMF9QrYTG5FcCWrQk+KXGu
79d0Lbi8YTCSLrGj5ycRGL3aJNvjdfznd7ukgMdLtF+iTopuMq+QhtXniIJ2Wo8Y
kIFRIcW49nwhagFkmcj6DRszRFF1kUELml8d8sMMxhydAgMBAAE=
-----END PUBLIC KEY-----";
        }

        _rsaPublic = RSA.Create();
        _rsaPublic.ImportFromPem(_publicKeyPem);
    }

    /// <summary>
    /// Конструктор з прямим публічним ключем (для незалежних тестів).
    /// </summary>
    public RsaPssLicenseValidationService(string publicKeyPem)
    {
        _publicKeyPem = publicKeyPem;
        _rsaPublic = RSA.Create();
        _rsaPublic.ImportFromPem(_publicKeyPem);
        _env = null!;
        _logger = null!;
    }

    public string GetPublicKeyPem() => _publicKeyPem;

    public LicenseValidationResult ValidateLicense(string licenseJson, byte[] signatureBytes)
    {
        if (string.IsNullOrWhiteSpace(licenseJson))
        {
            return new LicenseValidationResult(false, "EmptyLicense", "Вміст ліцензії порожній.");
        }

        if (signatureBytes == null || signatureBytes.Length == 0)
        {
            return new LicenseValidationResult(false, "MissingSignature", "Цифровий підпис ліцензії відсутній.");
        }

        // 1. Криптографічна перевірка підпису RSA-PSS (SHA-256)
        byte[] dataBytes = Encoding.UTF8.GetBytes(licenseJson);
        bool isSignatureValid = _rsaPublic.VerifyData(
            dataBytes,
            signatureBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss); // Використання сучасного режиму RSA-PSS з випадковою сіллю

        if (!isSignatureValid)
        {
            return new LicenseValidationResult(
                false, 
                "InvalidSignature", 
                "Криптографічний підпис недійсний! Файл ліцензії було несанкціоновано модифіковано або підроблено.");
        }

        // 2. Десеріалізація та бізнес-валідація параметрів ліцензії
        LicenseInfo? license;
        try
        {
            license = JsonSerializer.Deserialize<LicenseInfo>(licenseJson);
        }
        catch (JsonException ex)
        {
            return new LicenseValidationResult(false, "MalformedJson", $"Помилка формату ліцензійного файлу: {ex.Message}");
        }

        if (license == null)
        {
            return new LicenseValidationResult(false, "NullLicense", "Не вдалося розібрати параметри ліцензії.");
        }

        // Перевірка відповідності продукту
        if (!string.Equals(license.ProductId, ExpectedProductId, StringComparison.OrdinalIgnoreCase))
        {
            return new LicenseValidationResult(
                false,
                "ProductMismatch",
                $"Ліцензія належить іншому продукту: «{license.ProductId}» (очікується «{ExpectedProductId}»).",
                license);
        }

        // Перевірка часових рамок дії
        var now = DateTime.UtcNow;
        if (now < license.IssuedAt)
        {
            return new LicenseValidationResult(
                false,
                "Premature",
                $"Ліцензія ще не набрала чинності (дата видачі: {license.IssuedAt:yyyy-MM-dd}).",
                license);
        }

        if (now > license.ValidUntil)
        {
            return new LicenseValidationResult(
                false,
                "Expired",
                $"Термін дії ліцензії вичерпано ({license.ValidUntil:yyyy-MM-dd HH:mm}). Перейдіть на план продовження підписки.",
                license);
        }

        return new LicenseValidationResult(
            true,
            "Active",
            $"Ліцензія дійсна (Тариф: {license.Tier}, Власник: {license.Licensee}). Автентичність підтверджено цифровим підписом RSA-PSS.",
            license);
    }

    public LicenseValidationResult GetCurrentLicenseStatus()
    {
        try
        {
            string licensePath = Path.Combine(_env.ContentRootPath, "Licenses", "license.json");
            string sigPath = Path.Combine(_env.ContentRootPath, "Licenses", "license.sig");

            if (!File.Exists(licensePath) || !File.Exists(sigPath))
            {
                return new LicenseValidationResult(
                    true, 
                    "FreeCommunity", 
                    "Використовується базова безкоштовна версія (Freemium: до 100 треків, до 3 експортів).",
                    new LicenseInfo
                    {
                        ProductId = ExpectedProductId,
                        Tier = "Free",
                        Licensee = "Community User",
                        MaxTracks = 100,
                        MaxPlaylists = 3,
                        ValidUntil = DateTime.UtcNow.AddYears(10)
                    });
            }

            string licenseJson = File.ReadAllText(licensePath);
            byte[] signatureBytes = File.ReadAllBytes(sigPath);

            return ValidateLicense(licenseJson, signatureBytes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Помилка читання файлу ліцензії.");
            return new LicenseValidationResult(false, "ReadError", $"Помилка читання ліцензії: {ex.Message}");
        }
    }
}
