using System.IO;
using BaroManager.Data;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Services;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext db)
    {
        Directory.CreateDirectory(AppDbContext.DatabaseDirectory);

        db.Database.EnsureCreated();

        EnsureItemsColumns(db);
        EnsureCollectionsTables(db);
    }

    private static void EnsureItemsColumns(AppDbContext db)
    {
        TryExecuteSql(db, "ALTER TABLE items ADD COLUMN ExistsNow INTEGER NOT NULL DEFAULT 1;");
        TryExecuteSql(db, "ALTER TABLE items ADD COLUMN LastCheckedAt TEXT NULL;");
        TryExecuteSql(db, "ALTER TABLE items ADD COLUMN PathStatus TEXT NOT NULL DEFAULT 'Unknown';");

        TryExecuteSql(db, "CREATE INDEX IF NOT EXISTS IX_items_PathStatus ON items(PathStatus);");
        TryExecuteSql(db, "CREATE INDEX IF NOT EXISTS IX_items_Title ON items(Title);");
        TryExecuteSql(db, "CREATE INDEX IF NOT EXISTS IX_items_Path ON items(Path);");
        TryExecuteSql(db, "CREATE INDEX IF NOT EXISTS IX_items_ItemType ON items(ItemType);");
    }

    private static void EnsureCollectionsTables(AppDbContext db)
    {
        TryExecuteSql(db, """
            CREATE TABLE IF NOT EXISTS collections (
                Id INTEGER NOT NULL CONSTRAINT PK_collections PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );
        """);

        TryExecuteSql(db, "CREATE UNIQUE INDEX IF NOT EXISTS IX_collections_Name ON collections(Name);");

        TryExecuteSql(db, """
            CREATE TABLE IF NOT EXISTS item_collections (
                ManagedItemId INTEGER NOT NULL,
                CollectionId INTEGER NOT NULL,
                CONSTRAINT PK_item_collections PRIMARY KEY (ManagedItemId, CollectionId),
                CONSTRAINT FK_item_collections_items_ManagedItemId
                    FOREIGN KEY (ManagedItemId)
                    REFERENCES items (Id)
                    ON DELETE CASCADE,
                CONSTRAINT FK_item_collections_collections_CollectionId
                    FOREIGN KEY (CollectionId)
                    REFERENCES collections (Id)
                    ON DELETE CASCADE
            );
        """);

        TryExecuteSql(db, "CREATE INDEX IF NOT EXISTS IX_item_collections_CollectionId ON item_collections(CollectionId);");
    }

    private static void TryExecuteSql(AppDbContext db, string sql)
    {
        try
        {
            db.Database.ExecuteSqlRaw(sql);
        }
        catch
        {
            // ignored:
            // - duplicate column
            // - existing table/index
            // - old dev database quirks
        }
    }
}