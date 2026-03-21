using System.Text.Json;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed class AppConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configFilePath;

    public AppConfigService(string appDataPath)
    {
        Directory.CreateDirectory(appDataPath);
        _configFilePath = Path.Combine(appDataPath, "config.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configFilePath))
        {
            var config = new AppConfig();
            Save(config);
            return config;
        }

        var json = File.ReadAllText(_configFilePath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configFilePath, json);
    }

    public void Reset()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
    }
}
