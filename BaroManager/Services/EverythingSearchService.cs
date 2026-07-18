using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using BaroManager.Models;

namespace BaroManager.Services;

public static class EverythingSearchService
{
    public static List<EverythingSearchResult> Search(string query, int maxResults = 100)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<EverythingSearchResult>();

        maxResults = Math.Clamp(maxResults, 1, 1000);

        var settings = AppSettingsService.Load();
        var esPath = FindEsExecutable(settings);

        if (string.IsNullOrWhiteSpace(esPath))
        {
            throw new FileNotFoundException(LocalizationService.Get("Message.EsMissing"));
        }

        var result = RunEs(esPath, query, maxResults);

        if (result.ExitCode == 8 && settings.AutoStartEverything)
        {
            var started = TryStartEverything(settings);

            if (started)
            {
                for (var attempt = 0; attempt < 40 && result.ExitCode == 8; attempt++)
                {
                    Thread.Sleep(250);
                    result = RunEs(esPath, query, maxResults);
                }
            }
        }

        if (result.ExitCode != 0 && result.ExitCode != 1)
        {
            var error = string.IsNullOrWhiteSpace(result.Stderr)
                ? LocalizationService.Format("Message.EsExitCode", result.ExitCode)
                : result.Stderr.Trim();

            if (result.ExitCode == 8)
            {
                error = LocalizationService.Get("Message.EverythingUnavailable");
            }

            throw new InvalidOperationException(error);
        }

        return result.Stdout
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => File.Exists(x) || Directory.Exists(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => new EverythingSearchResult { Path = x })
            .ToList();
    }

    public static string GetResolvedEsPath()
    {
        return FindEsExecutable(AppSettingsService.Load()) ?? string.Empty;
    }

    public static string GetResolvedEverythingPath()
    {
        return FindEverythingExecutable(AppSettingsService.Load()) ?? string.Empty;
    }

    private static EsRunResult RunEs(string esPath, string query, int maxResults)
    {
        var args =
            $"-n {maxResults} " +
            "-timeout 3000 " +
            Quote(query.Trim());

        var psi = new ProcessStartInfo
        {
            FileName = esPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);

        if (process is null)
            throw new InvalidOperationException(LocalizationService.Get("Message.EsStartFailed"));

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(6000))
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // ignored
            }

            throw new TimeoutException(LocalizationService.Get("Message.EverythingTimeout"));
        }

        return new EsRunResult(process.ExitCode, stdout, stderr);
    }

    private static string? FindEsExecutable(AppSettings settings)
    {
        var candidates = new[]
        {
            Path.Combine(AppSettingsService.ToolsEverythingDirectory, "es.exe"),
            Path.Combine(AppContext.BaseDirectory, "everything", "es.exe"),
            Path.Combine(AppContext.BaseDirectory, "es.exe"),
            settings.EverythingEsPath,

            // dev fallback
            @"E:\ES\es.exe"
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }

        return FindInPath("es.exe");
    }

    private static string? FindEverythingExecutable(AppSettings settings)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var candidates = new[]
        {
            Path.Combine(AppSettingsService.ToolsEverythingDirectory, "Everything.exe"),
            Path.Combine(AppContext.BaseDirectory, "everything", "Everything.exe"),
            Path.Combine(AppContext.BaseDirectory, "Everything.exe"),
            settings.EverythingExePath,

            Path.Combine(programFiles, "Everything", "Everything.exe"),
            Path.Combine(programFilesX86, "Everything", "Everything.exe")
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }

        return FindInPath("Everything.exe");
    }

    private static bool TryStartEverything(AppSettings settings)
    {
        var everythingPath = FindEverythingExecutable(settings);

        if (string.IsNullOrWhiteSpace(everythingPath))
            return false;

        try
        {
            return EmbeddedEverythingService.Start(everythingPath);
        }
        catch
        {
            return false;
        }
    }

    private static string? FindInPath(string fileName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(pathVariable))
            return null;

        var paths = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in paths)
        {
            try
            {
                var candidate = Path.Combine(path.Trim(), fileName);

                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // ignored
            }
        }

        return null;
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private sealed record EsRunResult(int ExitCode, string Stdout, string Stderr);
}
