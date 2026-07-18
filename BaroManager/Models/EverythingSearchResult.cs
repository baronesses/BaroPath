using System.IO;

using BaroManager.Services;

namespace BaroManager.Models;

public class EverythingSearchResult
{
    public string Path { get; set; } = string.Empty;

    public string Name => string.IsNullOrWhiteSpace(Path)
        ? string.Empty
        : System.IO.Path.GetFileName(Path);

    public string DirectoryPath => string.IsNullOrWhiteSpace(Path)
        ? string.Empty
        : System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    public bool IsDirectory => Directory.Exists(Path);

    public bool IsFile => File.Exists(Path);

    public string ItemType
    {
        get
        {
            if (Directory.Exists(Path))
                return "Folder";

            var ext = System.IO.Path.GetExtension(Path).ToLowerInvariant();

            return ext switch
            {
                ".exe" => "App",
                ".bat" or ".cmd" or ".ps1" or ".py" => "Script",
                _ => "File"
            };
        }
    }

    public string TypeDisplay => ItemType switch
    {
        "Folder" => LocalizationService.Get("Type.Folder"),
        "File" => LocalizationService.Get("Type.File"),
        "App" => LocalizationService.Get("Type.App"),
        "Script" => LocalizationService.Get("Type.Script"),
        "Command" => LocalizationService.Get("Type.Command"),
        _ => $"❔ {ItemType}"
    };
}
