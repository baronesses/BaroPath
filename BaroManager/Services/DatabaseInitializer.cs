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
    }
}