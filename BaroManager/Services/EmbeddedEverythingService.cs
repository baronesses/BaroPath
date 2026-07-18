using System.Diagnostics;
using System.IO;

namespace BaroManager.Services;

public static class EmbeddedEverythingService
{
    private static bool _startedByBaroPath;

    public static string DataDirectory =>
        Path.Combine(AppSettingsService.SettingsDirectory, "everything");

    public static string ConfigurationPath =>
        Path.Combine(DataDirectory, "Everything.ini");

    public static bool Start(string everythingPath)
    {
        if (!File.Exists(everythingPath))
            return false;

        try
        {
            EnsureHiddenConfiguration();

            var startInfo = CreateStartInfo(everythingPath);
            startInfo.ArgumentList.Add("-config");
            startInfo.ArgumentList.Add(ConfigurationPath);
            startInfo.ArgumentList.Add("-startup");

            var process = Process.Start(startInfo);

            if (process is null)
                return false;

            _startedByBaroPath = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void Stop(string? everythingPath)
    {
        if (!_startedByBaroPath ||
            string.IsNullOrWhiteSpace(everythingPath) ||
            !File.Exists(everythingPath))
            return;

        try
        {
            var startInfo = CreateStartInfo(everythingPath);
            startInfo.ArgumentList.Add("-exit");

            using var process = Process.Start(startInfo);
            process?.WaitForExit(3000);
            _startedByBaroPath = false;
        }
        catch
        {
            // Shutdown must not be blocked by an optional search component.
        }
    }

    private static ProcessStartInfo CreateStartInfo(string everythingPath)
    {
        return new ProcessStartInfo
        {
            FileName = everythingPath,
            WorkingDirectory = Path.GetDirectoryName(everythingPath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    private static void EnsureHiddenConfiguration()
    {
        Directory.CreateDirectory(DataDirectory);

        var lines = File.Exists(ConfigurationPath)
            ? File.ReadAllLines(ConfigurationPath).ToList()
            : ["; Managed by BaroPath", "[Everything]"];

        SetValue(lines, "run_in_background", "1");
        SetValue(lines, "show_in_taskbar", "0");
        SetValue(lines, "show_tray_icon", "0");
        SetValue(lines, "check_for_updates_on_startup", "0");
        SetValue(lines, "allow_multiple_windows", "0");
        SetValue(lines, "allow_multiple_instances", "0");
        SetValue(lines, "allow_ipc", "1");
        SetValue(lines, "db_location", DataDirectory);

        File.WriteAllLines(ConfigurationPath, lines);
    }

    private static void SetValue(List<string> lines, string key, string value)
    {
        var prefix = key + "=";
        var index = lines.FindIndex(line =>
            line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        var newLine = prefix + value;

        if (index >= 0)
            lines[index] = newLine;
        else
            lines.Add(newLine);
    }
}
