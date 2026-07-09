using System.IO;
using System.Text;
using System.Text.Json;
using BaroManager.Data;
using BaroManager.Models;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Services;

public static class BackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static void Export(AppDbContext db, string filePath)
    {
        var document = new BackupDocument
        {
            App = "BaroManager",
            Version = 1,
            ExportedAt = DateTime.Now,

            Collections = db.ManagedCollections
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new CollectionBackupDto
                {
                    Name = x.Name,
                    SortOrder = x.SortOrder,
                    CreatedAt = x.CreatedAt
                })
                .ToList(),

            Items = db.ManagedItems
                .AsNoTracking()
                .Include(x => x.Collections)
                .ThenInclude(x => x.Collection)
                .OrderBy(x => x.Title)
                .Select(x => new ItemBackupDto
                {
                    Title = x.Title,
                    Path = x.Path,
                    ItemType = x.ItemType,
                    Arguments = x.Arguments,
                    WorkingDirectory = x.WorkingDirectory,
                    Tags = x.Tags,
                    Note = x.Note,
                    IsFavorite = x.IsFavorite,
                    RunOnAppStart = x.RunOnAppStart,
                    RunOnWindowsStartup = x.RunOnWindowsStartup,
                    ExistsNow = x.ExistsNow,
                    PathStatus = x.PathStatus,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    LastUsedAt = x.LastUsedAt,
                    LastCheckedAt = x.LastCheckedAt,
                    CollectionNames = x.Collections
                        .Select(c => c.Collection.Name)
                        .OrderBy(name => name)
                        .ToList()
                })
                .ToList()
        };

        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(document, JsonOptions);

        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    public static ImportBackupResult Import(AppDbContext db, string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Backup-файл не найден.", filePath);

        var json = File.ReadAllText(filePath, Encoding.UTF8);

        var document = JsonSerializer.Deserialize<BackupDocument>(json, JsonOptions);

        if (document is null)
            throw new InvalidOperationException("Не удалось прочитать backup-файл.");

        var result = new ImportBackupResult();

        ImportCollections(db, document, result);
        ImportItems(db, document, result);

        return result;
    }

    private static void ImportCollections(
        AppDbContext db,
        BackupDocument document,
        ImportBackupResult result)
    {
        var existingCollections = db.ManagedCollections
            .ToList()
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var collectionDto in document.Collections)
        {
            var name = collectionDto.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (existingCollections.ContainsKey(name))
                continue;

            var collection = new ManagedCollection
            {
                Name = name,
                SortOrder = collectionDto.SortOrder,
                CreatedAt = collectionDto.CreatedAt == default
                    ? DateTime.Now
                    : collectionDto.CreatedAt
            };

            db.ManagedCollections.Add(collection);
            existingCollections[name] = collection;
            result.CollectionsCreated++;
        }

        db.SaveChanges();
    }

    private static void ImportItems(
        AppDbContext db,
        BackupDocument document,
        ImportBackupResult result)
    {
        var collections = db.ManagedCollections
            .ToList()
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var itemDto in document.Items)
        {
            var cleanPath = itemDto.Path.Trim();

            if (string.IsNullOrWhiteSpace(cleanPath))
                continue;

            var item = db.ManagedItems
                .FirstOrDefault(x => x.Path == cleanPath);

            if (item is null)
            {
                item = new ManagedItem
                {
                    Title = string.IsNullOrWhiteSpace(itemDto.Title)
                        ? GuessTitle(cleanPath)
                        : itemDto.Title.Trim(),

                    Path = cleanPath,
                    ItemType = string.IsNullOrWhiteSpace(itemDto.ItemType)
                        ? "File"
                        : itemDto.ItemType.Trim(),

                    Arguments = itemDto.Arguments,
                    WorkingDirectory = itemDto.WorkingDirectory,
                    Tags = itemDto.Tags,
                    Note = itemDto.Note,
                    IsFavorite = itemDto.IsFavorite,
                    RunOnAppStart = itemDto.RunOnAppStart,
                    RunOnWindowsStartup = itemDto.RunOnWindowsStartup,
                    ExistsNow = itemDto.ExistsNow,
                    PathStatus = string.IsNullOrWhiteSpace(itemDto.PathStatus)
                        ? "Unknown"
                        : itemDto.PathStatus,

                    CreatedAt = itemDto.CreatedAt == default
                        ? DateTime.Now
                        : itemDto.CreatedAt,

                    UpdatedAt = itemDto.UpdatedAt,
                    LastUsedAt = itemDto.LastUsedAt,
                    LastCheckedAt = itemDto.LastCheckedAt
                };

                db.ManagedItems.Add(item);
                db.SaveChanges();

                result.ItemsCreated++;
            }
            else
            {
                item.Title = string.IsNullOrWhiteSpace(itemDto.Title)
                    ? item.Title
                    : itemDto.Title.Trim();

                item.ItemType = string.IsNullOrWhiteSpace(itemDto.ItemType)
                    ? item.ItemType
                    : itemDto.ItemType.Trim();

                item.Arguments = itemDto.Arguments;
                item.WorkingDirectory = itemDto.WorkingDirectory;
                item.Tags = itemDto.Tags;
                item.Note = itemDto.Note;
                item.IsFavorite = itemDto.IsFavorite;
                item.RunOnAppStart = itemDto.RunOnAppStart;
                item.RunOnWindowsStartup = itemDto.RunOnWindowsStartup;
                item.ExistsNow = itemDto.ExistsNow;

                item.PathStatus = string.IsNullOrWhiteSpace(itemDto.PathStatus)
                    ? item.PathStatus
                    : itemDto.PathStatus;

                item.UpdatedAt = DateTime.Now;
                item.LastUsedAt = itemDto.LastUsedAt;
                item.LastCheckedAt = itemDto.LastCheckedAt;

                db.SaveChanges();

                result.ItemsUpdated++;
            }

            foreach (var collectionNameRaw in itemDto.CollectionNames)
            {
                var collectionName = collectionNameRaw.Trim();

                if (string.IsNullOrWhiteSpace(collectionName))
                    continue;

                if (!collections.TryGetValue(collectionName, out var collection))
                    continue;

                var alreadyLinked = db.ItemCollections.Any(x =>
                    x.ManagedItemId == item.Id &&
                    x.CollectionId == collection.Id
                );

                if (alreadyLinked)
                    continue;

                db.ItemCollections.Add(new ManagedItemCollection
                {
                    ManagedItemId = item.Id,
                    CollectionId = collection.Id
                });

                result.LinksCreated++;
            }

            db.SaveChanges();
        }
    }

    private static string GuessTitle(string path)
    {
        var clean = path.TrimEnd('\\', '/');

        var name = Path.GetFileName(clean);

        return string.IsNullOrWhiteSpace(name)
            ? clean
            : name;
    }

    private sealed class BackupDocument
    {
        public string App { get; set; } = "BaroManager";
        public int Version { get; set; } = 1;
        public DateTime ExportedAt { get; set; } = DateTime.Now;
        public List<CollectionBackupDto> Collections { get; set; } = new();
        public List<ItemBackupDto> Items { get; set; } = new();
    }

    private sealed class CollectionBackupDto
    {
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class ItemBackupDto
    {
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ItemType { get; set; } = "File";
        public string? Arguments { get; set; }
        public string? WorkingDirectory { get; set; }
        public string? Tags { get; set; }
        public string? Note { get; set; }
        public bool IsFavorite { get; set; }
        public bool RunOnAppStart { get; set; }
        public bool RunOnWindowsStartup { get; set; }
        public bool ExistsNow { get; set; } = true;
        public string PathStatus { get; set; } = "Unknown";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public List<string> CollectionNames { get; set; } = new();
    }
}

public sealed class ImportBackupResult
{
    public int CollectionsCreated { get; set; }
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int LinksCreated { get; set; }
}