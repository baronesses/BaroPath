using BaroManager.Data;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Services;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext db)
    {
        db.Database.EnsureCreated();

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS collections (
    Id int NOT NULL AUTO_INCREMENT,
    Name varchar(255) NOT NULL,
    SortOrder int NOT NULL DEFAULT 0,
    CreatedAt datetime(6) NOT NULL,
    PRIMARY KEY (Id),
    INDEX IX_collections_Name (Name)
) CHARACTER SET utf8mb4;
");

        db.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS item_collections (
    managed_item_id int NOT NULL,
    collection_id int NOT NULL,
    PRIMARY KEY (managed_item_id, collection_id),
    CONSTRAINT fk_item_collections_items
        FOREIGN KEY (managed_item_id)
        REFERENCES items (Id)
        ON DELETE CASCADE,
    CONSTRAINT fk_item_collections_collections
        FOREIGN KEY (collection_id)
        REFERENCES collections (Id)
        ON DELETE CASCADE
) CHARACTER SET utf8mb4;
");

        TryExecuteSql(db, "ALTER TABLE items ADD COLUMN ExistsNow tinyint(1) NOT NULL DEFAULT 1;");
        TryExecuteSql(db, "ALTER TABLE items ADD COLUMN LastCheckedAt datetime(6) NULL;");
        TryExecuteSql(db, "ALTER TABLE items ADD COLUMN PathStatus varchar(50) NOT NULL DEFAULT 'Unknown';");
        TryExecuteSql(db, "ALTER TABLE items ADD INDEX IX_items_PathStatus (PathStatus);");
    }

    private static void TryExecuteSql(AppDbContext db, string sql)
    {
        try
        {
            db.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception ex)
        {
            var message = ex.Message.ToLowerInvariant();

            if (message.Contains("duplicate column") ||
                message.Contains("duplicate key") ||
                message.Contains("already exists"))
            {
                return;
            }

            throw;
        }
    }
}