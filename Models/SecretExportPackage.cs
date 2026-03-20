namespace PassTypePro.Models;

public sealed class SecretExportPackage
{
    public string Format { get; set; } = "PassTypePro.SecretExport.v1";
    public string CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    public List<SecretEntry> Secrets { get; set; } = [];
}
