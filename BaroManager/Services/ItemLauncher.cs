using System.Diagnostics;
using BaroManager.Models;
using System.IO;

namespace BaroManager.Services;

public static class ItemLauncher
{
    public static void Open(ManagedItem item)
    {
        if (item.ItemType == "Command")
        {
            Run(item);
            return;
        }

        EnsurePathExists(item.Path);

        var psi = new ProcessStartInfo
        {
            FileName = item.Path,
            UseShellExecute = true
        };

        Process.Start(psi);
    }

    public static void OpenInExplorer(ManagedItem item)
    {
        if (Directory.Exists(item.Path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{item.Path}\"",
                UseShellExecute = true
            });

            return;
        }

        if (File.Exists(item.Path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{item.Path}\"",
                UseShellExecute = true
            });

            return;
        }

        if (!string.IsNullOrWhiteSpace(item.WorkingDirectory) &&
            Directory.Exists(item.WorkingDirectory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{item.WorkingDirectory}\"",
                UseShellExecute = true
            });

            return;
        }

        throw new FileNotFoundException("Путь не найден.", item.Path);
    }

    public static void Run(ManagedItem item)
    {
        if (item.ItemType == "Command")
        {
            var command = item.Path;

            if (!string.IsNullOrWhiteSpace(item.Arguments))
                command += " " + item.Arguments;

            var cmdPsi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                UseShellExecute = false
            };

            SetWorkingDirectory(cmdPsi, item);
            Process.Start(cmdPsi);
            return;
        }

        EnsurePathExists(item.Path);

        var psi = new ProcessStartInfo
        {
            FileName = item.Path,
            Arguments = item.Arguments ?? string.Empty,
            UseShellExecute = true
        };

        SetWorkingDirectory(psi, item);
        Process.Start(psi);
    }

    private static void SetWorkingDirectory(ProcessStartInfo psi, ManagedItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.WorkingDirectory) &&
            Directory.Exists(item.WorkingDirectory))
        {
            psi.WorkingDirectory = item.WorkingDirectory;
            return;
        }

        if (File.Exists(item.Path))
        {
            var dir = System.IO.Path.GetDirectoryName(item.Path);

            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                psi.WorkingDirectory = dir;
        }
        else if (Directory.Exists(item.Path))
        {
            psi.WorkingDirectory = item.Path;
        }
    }

    private static void EnsurePathExists(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
            return;

        throw new FileNotFoundException("Путь не найден.", path);
    }
}