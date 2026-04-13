using MarketplaceArtesanato.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace MarketplaceArtesanato.Data.Data;

public class ArtesianDbContext : DbContext
{
    public ArtesianDbContext(DbContextOptions<ArtesianDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<SellerSubscription> SellerSubscriptions => Set<SellerSubscription>();
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
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<StripeEventLog> StripeEventLogs { get; set; } = null!;

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

        modelBuilder.Entity<SellerSubscription>()
            .HasIndex(ss => ss.SellerId)
            .IsUnique();
        modelBuilder.Entity<StripeEventLog>()
            .HasIndex(e => e.EventId)
            .IsUnique();

        modelBuilder.Entity<SellerSubscription>()
            .Property(ss => ss.CommissionRate)
            .HasPrecision(5, 2);
        modelBuilder.Entity<SellerSubscription>()
             .HasIndex(ss => ss.Plan);

        modelBuilder.Entity<Seller>()
            .HasOne(s => s.Subscription)
            .WithOne(ss => ss.Seller)
            .HasForeignKey<SellerSubscription>(ss => ss.SellerId)
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

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => oi.SellerId)
            .HasDatabaseName("IX_OrderItems_SellerId");

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => new { oi.SellerId, oi.ProductId })
            .HasDatabaseName("IX_OrderItems_SellerId_ProductId");

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => new { oi.SellerId, oi.CreatedAt })
            .HasDatabaseName("IX_OrderItems_SellerId_CreatedAt");

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


        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.OriginalPrice)
                .HasPrecision(18, 2)
                .IsRequired(false);

            entity.Property(p => p.HasDiscount)
                .HasDefaultValue(false)
                .IsRequired();
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.Property(s => s.PlanDiscountPercentage)
                .HasPrecision(5, 2)
                .HasDefaultValue(0)
                .IsRequired();
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .HasMaxLength(1000);

            entity.Property(p => p.DiscountPercentage)
                .HasPrecision(5, 2)
                .IsRequired();

            entity.Property(p => p.ProductIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                )
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            entity.Property(p => p.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(p => p.StartDate)
                .IsRequired();

            entity.Property(p => p.EndDate)
                .IsRequired();

            entity.HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => p.SellerId)
                .HasDatabaseName("IX_Promotions_SellerId");

            entity.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Promotions_IsActive");

            entity.HasIndex(p => new { p.IsActive, p.StartDate, p.EndDate })
                .HasDatabaseName("IX_Promotions_IsActive_Dates");
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Description)
                .HasMaxLength(1000);

            entity.Property(c => c.DiscountPercentage)
                .HasPrecision(5, 2)
                .IsRequired();

            entity.Property(c => c.CategoryIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                )
                .HasColumnType("nvarchar(max)");

            entity.Property(c => c.SellerIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                )
                .HasColumnType("nvarchar(max)");

            entity.Property(c => c.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(c => c.StartDate)
                .IsRequired();

            entity.Property(c => c.EndDate)
                .IsRequired();

            entity.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Campaigns_IsActive");

            entity.HasIndex(c => new { c.IsActive, c.StartDate, c.EndDate })
                .HasDatabaseName("IX_Campaigns_IsActive_Dates");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.Type)
                .IsRequired()
                .HasDefaultValue(CouponType.Platform);

            entity.Property(c => c.DiscountType)
                .IsRequired()
                .HasDefaultValue(DiscountType.Percentage);

            entity.Property(c => c.DiscountValue)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(c => c.MaxDiscount)
                .HasPrecision(18, 2)
                .IsRequired(false);

            entity.Property(c => c.MinOrderValue)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            entity.Property(c => c.Scope)
                .IsRequired()
                .HasDefaultValue(CouponScope.EntireOrder);

            entity.Property(c => c.PlatformSharePercentage)
                .HasPrecision(5, 2)
                .IsRequired(false);

            entity.Property(c => c.ValidFrom)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(c => c.ValidUntil)
                .IsRequired();

            entity.Property(c => c.UsageLimit)
                .HasDefaultValue(0);

            entity.Property(c => c.UsageCount)
                .HasDefaultValue(0);

            entity.Property(c => c.UsageLimitPerUser)
                .HasDefaultValue(1);

            entity.Property(c => c.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(c => c.PreventsCombination)
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(c => c.OnlyWithoutPromotion)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(c => c.OnlyFirstPurchase)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(c => c.IsDeleted)
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasIndex(c => c.Code)
                .IsUnique()
                .HasDatabaseName("IX_Coupons_Code");

            entity.HasIndex(c => new { c.IsActive, c.ValidUntil })
                .HasDatabaseName("IX_Coupons_IsActive_ValidUntil");

            entity.HasIndex(c => c.Type)
                .HasDatabaseName("IX_Coupons_Type");

            entity.HasIndex(c => c.CreatorSellerId)
                .HasDatabaseName("IX_Coupons_CreatorSellerId");
        });

        modelBuilder.Entity<CouponUsage>(entity =>
        {
            entity.HasKey(cu => cu.Id);

            entity.Property(cu => cu.DiscountApplied)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(cu => cu.PaidBy)
                .IsRequired()
                .HasDefaultValue(DiscountPaidBy.Platform);

            entity.Property(cu => cu.PlatformPaid)
                .HasPrecision(18, 2)
                .IsRequired(false);

            entity.Property(cu => cu.SellerPaid)
                .HasPrecision(18, 2)
                .IsRequired(false);

            entity.Property(cu => cu.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(cu => cu.IsDeleted)
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasOne(cu => cu.Coupon)
                .WithMany(c => c.Usages)
                .HasForeignKey(cu => cu.CouponId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(cu => new { cu.CouponId, cu.UserId })
                .HasDatabaseName("IX_CouponUsages_CouponId_UserId");

            entity.HasIndex(cu => cu.UserId)
                .HasDatabaseName("IX_CouponUsages_UserId");

            entity.HasIndex(cu => cu.OrderId)
                .HasDatabaseName("IX_CouponUsages_OrderId");
        });

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