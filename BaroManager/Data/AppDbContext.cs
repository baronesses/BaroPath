using System.IO;
using BaroManager.Models;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Data;

public class AppDbContext : DbContext
{
    public DbSet<ManagedItem> ManagedItems => Set<ManagedItem>();
    public DbSet<ManagedCollection> ManagedCollections => Set<ManagedCollection>();
    public DbSet<ManagedItemCollection> ItemCollections => Set<ManagedItemCollection>();

    public static string DatabaseDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "BaroManager");
        }
    }

    public static string DatabasePath => Path.Combine(DatabaseDirectory, "baromanager.db");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Directory.CreateDirectory(DatabaseDirectory);

        optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagedItem>(entity =>
        {
            entity.ToTable("items");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.Path)
                .IsRequired();

            entity.Property(x => x.ItemType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.Arguments);
            entity.Property(x => x.WorkingDirectory);
            entity.Property(x => x.Tags);
            entity.Property(x => x.Note);

            entity.Property(x => x.IsFavorite)
                .HasDefaultValue(false);

            entity.Property(x => x.RunOnAppStart)
                .HasDefaultValue(false);

            entity.Property(x => x.RunOnWindowsStartup)
                .HasDefaultValue(false);

            entity.Property(x => x.ExistsNow)
                .HasDefaultValue(true);

            entity.Property(x => x.PathStatus)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Unknown");

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt);
            entity.Property(x => x.LastUsedAt);
            entity.Property(x => x.LastCheckedAt);

            entity.HasIndex(x => x.Title);
            entity.HasIndex(x => x.Path);
            entity.HasIndex(x => x.ItemType);
            entity.HasIndex(x => x.PathStatus);
        });

        modelBuilder.Entity<ManagedCollection>(entity =>
        {
            entity.ToTable("collections");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<ManagedItemCollection>(entity =>
        {
            entity.ToTable("item_collections");

            entity.HasKey(x => new
            {
                x.ManagedItemId,
                x.CollectionId
            });

            entity.HasOne(x => x.ManagedItem)
                .WithMany(x => x.Collections)
                .HasForeignKey(x => x.ManagedItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Collection)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}