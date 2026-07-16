namespace BaroManager.Models;

public class AppSettings
{
    public string EverythingEsPath { get; set; } = string.Empty;
    public string EverythingExePath { get; set; } = string.Empty;
    public bool AutoStartEverything { get; set; } = true;

    public string Language { get; set; } = "ru";
    public string Theme { get; set; } = "dark";
    public string ItemViewMode { get; set; } = "List";
}
