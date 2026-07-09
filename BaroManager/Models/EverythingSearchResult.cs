using System.IO;

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
        "Folder" => "📁 Folder",
        "File" => "📄 File",
        "App" => "🚀 App",
        "Script" => "⚙ Script",
        "Command" => "⌨ Command",
        _ => $"❔ {ItemType}"
    };
}