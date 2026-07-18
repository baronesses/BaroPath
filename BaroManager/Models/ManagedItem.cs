using System.ComponentModel.DataAnnotations.Schema;

using System.Windows.Media;
using BaroManager.Services;

namespace BaroManager.Models;

public class ManagedItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    // Folder, File, App, Script, Command
    public string ItemType { get; set; } = "Folder";

    public string? Arguments { get; set; }

    public string? WorkingDirectory { get; set; }

    public string? Tags { get; set; }

    public string? Note { get; set; }

    public string? IconPath { get; set; }

    public bool IsFavorite { get; set; }

    public bool RunOnAppStart { get; set; }

    public bool RunOnWindowsStartup { get; set; }

    public bool ExistsNow { get; set; } = true;

    public DateTime? LastCheckedAt { get; set; }

    // Unknown, OK, Missing, WorkDirMissing, Command
    public string PathStatus { get; set; } = "Unknown";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public ICollection<ManagedItemCollection> Collections { get; set; } = new List<ManagedItemCollection>();

    [NotMapped]
    public ImageSource IconSource => ItemIconService.GetIcon(this);

    [NotMapped]
    public string StatusText => PathStatus switch
    {
        "OK" => LocalizationService.Get("Status.OK"),
        "Missing" => LocalizationService.Get("Status.Missing"),
        "WorkDirMissing" => LocalizationService.Get("Status.WorkDirMissing"),
        "Command" => LocalizationService.Get("Status.Command"),
        _ => LocalizationService.Get("Status.Unknown")
    };

    [NotMapped]
    public string StatusDisplay => PathStatus switch
    {
        "OK" => LocalizationService.Get("Status.OK"),
        "Missing" => LocalizationService.Get("Status.Missing"),
        "WorkDirMissing" => LocalizationService.Get("Status.WorkDirMissing"),
        "Command" => LocalizationService.Get("Status.Command"),
        _ => LocalizationService.Get("Status.Unknown")
    };

    [NotMapped]
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
