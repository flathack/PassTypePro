using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed class SecretImportExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Export(string filePath, IReadOnlyCollection<SecretEntry> secrets, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            throw new InvalidOperationException("Eine Export-Passphrase ist erforderlich.");
        }

        var package = new SecretExportPackage
        {
            Secrets = secrets.ToList()
        };

        var json = JsonSerializer.Serialize(package, JsonOptions);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var exportPayload = string.Join(
            '.',
            "PTP1",
            Convert.ToBase64String(salt),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(cipherBytes));

        File.WriteAllText(filePath, exportPayload, Encoding.UTF8);
    }

    public IReadOnlyList<SecretEntry> Import(string filePath, string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            throw new InvalidOperationException("Eine Import-Passphrase ist erforderlich.");
        }

        var content = File.ReadAllText(filePath, Encoding.UTF8).Trim();
        var parts = content.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5 || !string.Equals(parts[0], "PTP1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Das Importformat wird nicht unterstuetzt.");
        }

        var salt = Convert.FromBase64String(parts[1]);
        var nonce = Convert.FromBase64String(parts[2]);
        var tag = Convert.FromBase64String(parts[3]);
        var cipherBytes = Convert.FromBase64String(parts[4]);
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        var json = Encoding.UTF8.GetString(plainBytes);
        var package = JsonSerializer.Deserialize<SecretExportPackage>(json, JsonOptions)
            ?? throw new InvalidOperationException("Die Importdatei konnte nicht gelesen werden.");

        if (!string.Equals(package.Format, "PassTypePro.SecretExport.v1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Die Importdatei hat ein unbekanntes Format.");
        }

        return package.Secrets;
    }
}
