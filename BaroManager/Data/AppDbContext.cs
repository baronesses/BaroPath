using BaroManager.Models;
using Microsoft.EntityFrameworkCore;

namespace BaroManager.Data;

public class AppDbContext : DbContext
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=baromanager;User=baro;Password=baro12345;";

    public DbSet<ManagedItem> ManagedItems => Set<ManagedItem>();
    
    public DbSet<ManagedCollection> ManagedCollections => Set<ManagedCollection>();

    public DbSet<ManagedItemCollection> ItemCollections => Set<ManagedItemCollection>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(
            ConnectionString,
            ServerVersion.AutoDetect(ConnectionString)
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagedItem>(entity =>
        {
            entity.ToTable("items");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.Path)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(x => x.ItemType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Arguments)
                .HasMaxLength(2000);

            entity.Property(x => x.WorkingDirectory)
                .HasMaxLength(2000);

            entity.Property(x => x.Tags)
                .HasMaxLength(1000);

            entity.Property(x => x.Note)
                .HasColumnType("text");

            entity.HasIndex(x => x.Title);
            entity.HasIndex(x => x.Path);
            entity.HasIndex(x => x.ItemType);
        });
        
        modelBuilder.Entity<ManagedCollection>(entity =>
        {
            entity.ToTable("collections");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<ManagedItemCollection>(entity =>
        {
            entity.ToTable("item_collections");

            entity.HasKey(x => new { x.ManagedItemId, x.CollectionId });

            entity.Property(x => x.ManagedItemId)
                .HasColumnName("managed_item_id");

            entity.Property(x => x.CollectionId)
                .HasColumnName("collection_id");

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