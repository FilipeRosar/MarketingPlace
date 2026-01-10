using MarketplaceArtesanato.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace MarketplaceArtesanato.Data.Data;

public class ArtesianDbContext : DbContext
{
    public ArtesianDbContext(DbContextOptions<ArtesianDbContext> options) : base(options) { }

    // --- DbSets (Tabelas) ---
    public DbSet<User> Users => Set<User>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Admin> Admins => Set<Admin>(); // Nova tabela Admin

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStoryMedia> ProductStoryMedia => Set<ProductStoryMedia>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<Moment> Moments { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<User>()
            .HasOne(u => u.SellerProfile)
            .WithOne(s => s.User)
            .HasForeignKey<Seller>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.CustomerProfile)
            .WithOne(c => c.User)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.AdminProfile)
            .WithOne(a => a.User)
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<User>()
            .HasOne(u => u.Address) 
            .WithOne()
            .HasForeignKey<User>(u => u.AddressId)
            .OnDelete(DeleteBehavior.SetNull); 


        modelBuilder.Entity<Product>()
            .HasOne(p => p.Seller)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Restrict); 

        modelBuilder.Entity<Product>()
            .HasMany(p => p.Images)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<Order>()
            .HasOne(o => o.Buyer) 
            .WithMany(u => u.OrdersAsBuyer)
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
             .HasIndex(c => c.UserId)
             .IsUnique();

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

        modelBuilder.Entity<Moment>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Description).IsRequired().HasMaxLength(500);
            entity.Property(m => m.VideoUrl).IsRequired();
            entity.HasOne(m => m.Seller)
                  .WithMany(s => s.Moments)
                  .HasForeignKey(m => m.SellerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Seller>().Property(p => p.CommissionRate).HasPrecision(18, 2);
        modelBuilder.Entity<Seller>().Property(p => p.RatingAverage).HasPrecision(2, 1); 

        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 2); 

        modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.SellerCommissionsJson)
            .HasColumnName("SellerCommissions")
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        // --- 4. FILTRO DE SOFT DELETE GLOBAL ---
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

// Extensão do Soft Delete (Mantida igual)
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