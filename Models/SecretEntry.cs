namespace PassTypePro.Models;

public sealed class SecretEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string TotpSeed { get; set; } = string.Empty;
    public string SequenceTemplate { get; set; } = "{SECRET}";
    public string SecretHotkey { get; set; } = string.Empty;
    public int StartDelayMs { get; set; } = 150;
    public int KeystrokeDelayMs { get; set; } = 0;
    public bool IsPrimary { get; set; }

    public override string ToString()
    {
        var label = IsPrimary ? $"{Name} (Primary)" : Name;

        if (!string.IsNullOrWhiteSpace(SecretHotkey))
        {
            label += $" [{SecretHotkey}]";
        }

        if (StartDelayMs > 0)
        {
            label += $" ({StartDelayMs} ms)";
        }

        if (KeystrokeDelayMs > 0)
        {
            label += $" <{KeystrokeDelayMs} ms/Zeichen>";
        }

        return label;
    }
}
