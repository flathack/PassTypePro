namespace PassTypePro.Models;

public sealed class SettingsUpdate
{
    public AppConfig Config { get; init; } = new();
    public string? NewPin { get; init; }
}
