using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed class SecureSecretStore
{
    private readonly string _filePath;

    public SecureSecretStore(string appDataPath)
    {
        Directory.CreateDirectory(appDataPath);
        _filePath = Path.Combine(appDataPath, "secrets.dat");
    }

    public IReadOnlyList<SecretEntry> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(_filePath);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(bytes);

            return JsonSerializer.Deserialize<List<SecretEntry>>(json) ?? [];
        }
        catch (Exception ex)
        {
            var backupPath = BackupCorruptFile();
            throw new InvalidOperationException(
                $"Die Secret-Datei konnte nicht geladen werden. Eine Sicherung wurde erstellt: {backupPath}",
                ex);
        }
    }

    public void Save(IEnumerable<SecretEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries);
        var bytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, protectedBytes);
    }

    public void Reset()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private string BackupCorruptFile()
    {
        var backupPath = _filePath + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
        File.Copy(_filePath, backupPath, overwrite: true);
        return backupPath;
    }
}
