using ExtensionesShop.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ExtensionesShop.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Category ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.Slug).IsUnique();
        });

        // ── Product ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Slug).HasMaxLength(200).IsRequired();
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.OriginalPrice).HasPrecision(18, 2);
            entity.Property(p => p.Description).HasMaxLength(4000);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Seed Data ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Clip-In", Slug = "clip-in", SortOrder = 1,
                Description = "Sin químicos ni calor. Perfectas para uso diario." },
            new Category { Id = 2, Name = "Tape-In", Slug = "tape-in", SortOrder = 2,
                Description = "Resultado de salón, duración de semanas." },
            new Category { Id = 3, Name = "Keratina", Slug = "keratin", SortOrder = 3,
                Description = "Fusión perfecta y natural." }
        );
    }
}
