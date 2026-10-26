using LikedByGenre.Models;

namespace LikedByGenre.Services;

/// <summary>
/// Інтерфейс служби перевірки автентичності та валідності цифрових ліцензій (RSA-PSS).
/// </summary>
public interface ILicenseValidationService
{
    /// <summary>
    /// Перевіряє цифровий підпис RSA-PSS (SHA-256) та строки дії довільного вмісту ліцензії.
    /// </summary>
    LicenseValidationResult ValidateLicense(string licenseJson, byte[] signatureBytes);

    /// <summary>
    /// Перевіряє поточну активну ліцензію системи з каталогу Licenses/.
    /// </summary>
    LicenseValidationResult GetCurrentLicenseStatus();

    /// <summary>
    /// Отримує публічний ключ перевірки у форматі PEM.
    /// </summary>
    string GetPublicKeyPem();
}
