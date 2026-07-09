using System.IO;
using System.Text.Json;
using BaroManager.Models;

namespace BaroManager.Services;

public static class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SettingsDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "BaroManager");
        }
    }

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static string ToolsEverythingDirectory =>
        Path.Combine(AppContext.BaseDirectory, "tools", "everything");

    public static AppSettings Load()
    {
        Directory.CreateDirectory(SettingsDirectory);

        if (!File.Exists(SettingsPath))
        {
            var defaults = CreateDefault();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            return settings ?? CreateDefault();
        }
        catch
        {
            var defaults = CreateDefault();
            Save(defaults);
            return defaults;
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static AppSettings CreateDefault()
    {
        var bundledEs = Path.Combine(ToolsEverythingDirectory, "es.exe");
        var bundledEverything = Path.Combine(ToolsEverythingDirectory, "Everything.exe");

        return new AppSettings
        {
            EverythingEsPath = File.Exists(bundledEs) ? bundledEs : string.Empty,
            EverythingExePath = File.Exists(bundledEverything) ? bundledEverything : string.Empty,
            AutoStartEverything = true,
            Language = "ru",
            Theme = "dark"
        };
    }
}