using System.ComponentModel.DataAnnotations.Schema;

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
    public string StatusText => PathStatus switch
    {
        "OK" => "OK",
        "Missing" => "Missing",
        "WorkDirMissing" => "Work dir missing",
        "Command" => "Command",
        _ => "Unknown"
    };
}