using BaroManager.Data;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Services;

public static class StartupItemRunner
{
    public static void RunStartupItems(AppDbContext db)
    {
        var items = db.ManagedItems
            .AsNoTracking()
            .Where(x => x.RunOnAppStart)
            .ToList();

        foreach (var item in items)
        {
            try
            {
                ItemLauncher.Run(item);
            }
            catch
            {
                // Пока молча пропускаем, чтобы один битый путь не убивал приложение.
                // Потом сделаем нормальный лог ошибок.
            }
        }
    }
}