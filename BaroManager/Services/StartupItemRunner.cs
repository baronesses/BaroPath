using BaroManager.Data;
using BaroManager.Models;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Services;

public static class StartupItemRunner
{
    public static StartupRunResult RunStartupItems(AppDbContext db)
    {
        var result = new StartupRunResult();

        var items = db.ManagedItems
            .AsNoTracking()
            .Where(x => x.RunOnAppStart)
            .OrderBy(x => x.Title)
            .ToList();

        result.Total = items.Count;

        foreach (var item in items)
        {
            try
            {
                if (item.PathStatus == "Missing")
                {
                    result.Skipped.Add($"{item.Title} — путь отсутствует");
                    continue;
                }

                if (item.PathStatus == "WorkDirMissing")
                {
                    result.Skipped.Add($"{item.Title} — рабочая папка отсутствует");
                    continue;
                }

                ItemLauncher.Run(item);
                result.Started.Add(item.Title);
            }
            catch (Exception ex)
            {
                result.Failed.Add($"{item.Title} — {ex.Message}");
            }
        }

        return result;
    }
}

public class StartupRunResult
{
    public int Total { get; set; }

    public List<string> Started { get; } = new();

    public List<string> Skipped { get; } = new();

    public List<string> Failed { get; } = new();

    public bool HasProblems => Skipped.Count > 0 || Failed.Count > 0;
}