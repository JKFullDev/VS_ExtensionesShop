using ExtensionesShop.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ExtensionesShop.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<CartItemEntity> CartItems => Set<CartItemEntity>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Actualizar UpdatedAt automáticamente para CartItems
        var entries = ChangeTracker.Entries<CartItemEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Category ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
        });

        // ── Subcategory ───────────────────────────────────────────────────────
        modelBuilder.Entity<Subcategory>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();

            entity.HasOne(s => s.Category)
                  .WithMany(c => c.Subcategories)
                  .HasForeignKey(s => s.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Product ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.ImageUrl).HasMaxLength(255);
            entity.Property(p => p.Color).HasMaxLength(50);
            entity.Property(p => p.Centimeters).HasPrecision(5, 2);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Subcategory)
                  .WithMany(s => s.Products)
                  .HasForeignKey(p => p.SubcategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.Address).HasMaxLength(255);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.Province).HasMaxLength(100);
            entity.Property(u => u.PostalCode).HasMaxLength(10);

            entity.HasIndex(u => u.Email).IsUnique();
        });

        // ── Favorite ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.ToTable("Favorites");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(f => f.User)
                  .WithMany()
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Product)
                  .WithMany()
                  .HasForeignKey(f => f.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => new { f.UserId, f.ProductId })
                  .IsUnique()
                  .HasDatabaseName("UQ_Favorites_UserProduct");
        });

        // ── CartItemEntity ────────────────────────────────────────────────────
        modelBuilder.Entity<CartItemEntity>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Quantity)
                  .HasDefaultValue(1);

            entity.Property(c => c.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(c => c.UpdatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Product)
                  .WithMany()
                  .HasForeignKey(c => c.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => new { c.UserId, c.ProductId })
                  .IsUnique()
                  .HasDatabaseName("UQ_CartItems_UserProduct");
        });

        // ── Order ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CustomerEmail).HasMaxLength(100).IsRequired();
            entity.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(o => o.CustomerPhone).HasMaxLength(20).IsRequired();
            entity.Property(o => o.ShippingAddress).HasMaxLength(255).IsRequired();
            entity.Property(o => o.City).HasMaxLength(100).IsRequired();
            entity.Property(o => o.Province).HasMaxLength(100);
            entity.Property(o => o.PostalCode).HasMaxLength(10).IsRequired();
            entity.Property(o => o.Subtotal).HasPrecision(18, 2);
            entity.Property(o => o.ShippingCost).HasPrecision(18, 2);
            entity.Property(o => o.Total).HasPrecision(18, 2);

            entity.HasOne(o => o.User)
                  .WithMany()
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.CreatedAt);
            entity.HasIndex(o => o.Status);
        });

        // ── OrderItem ─────────────────────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            entity.Property(oi => oi.SelectedColor).HasMaxLength(50);
            entity.Property(oi => oi.SelectedCentimeters).HasPrecision(5, 2);

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany()
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.ProductId);
        });

        // ── ProductVariant ────────────────────────────────────────────────────
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(pv => pv.Id);
            entity.Property(pv => pv.Color).HasMaxLength(50);
            entity.Property(pv => pv.Centimeters).HasPrecision(5, 2);
            entity.Property(pv => pv.Price).HasPrecision(18, 2);
            entity.Property(pv => pv.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
            entity.Property(pv => pv.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(pv => pv.Product)
                  .WithMany(p => p.Variants)
                  .HasForeignKey(pv => pv.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pv => pv.ProductId);
        });

        // ── ProductImage ──────────────────────────────────────────────────────
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.ImageUrl).HasMaxLength(int.MaxValue).IsRequired();
            entity.Property(pi => pi.AltText).HasMaxLength(255);
            entity.Property(pi => pi.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(pi => pi.Product)
                  .WithMany(p => p.Images)
                  .HasForeignKey(pi => pi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.ProductVariant)
                  .WithMany(pv => pv.Images)
                  .HasForeignKey(pi => pi.ProductVariantId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pi => pi.ProductId);
            entity.HasIndex(pi => pi.ProductVariantId);
            entity.HasIndex(pi => new { pi.ProductId, pi.DisplayOrder });
        });
    }
}

