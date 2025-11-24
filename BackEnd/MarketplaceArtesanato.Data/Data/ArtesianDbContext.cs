using MarketplaceArtesanato.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MarketplaceArtesanato.Data.Data;

public class ArtesianDbContext : DbContext
{
    public ArtesianDbContext(DbContextOptions<ArtesianDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Seller)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Cart>()
            .HasOne(c => c.Customer)
            .WithOne(c => c.Cart)
            .HasForeignKey<Cart>(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Customer)
            .WithMany(c => c.Ratings)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Ratings)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<Order>()
            .Property(o => o.SellerCommissionsJson)
            .HasColumnName("SellerCommissions")
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);

        modelBuilder.Entity<Product>().HasIndex(p => p.Category);
        modelBuilder.Entity<Product>().HasIndex(p => p.Status);
        modelBuilder.Entity<Cart>().HasIndex(c => c.CustomerId).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(o => o.BuyerId);
        modelBuilder.Entity<OrderItem>().HasIndex(oi => oi.OrderId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                entityType.SetQueryFilterSoftDelete();
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}

public static class ModelBuilderExtensions
{
    public static void SetQueryFilterSoftDelete(this IMutableEntityType entityType)
    {
        var method = typeof(ModelBuilderExtensions)
            .GetMethod(nameof(GetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType.ClrType);

        var filter = method.Invoke(null, null)!;
        entityType.SetQueryFilter((LambdaExpression)filter);
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>() where TEntity : BaseEntity
    {
        return (TEntity e) => !e.IsDeleted;
    }
}