using MarketplaceArtesanato.API.Authorization;
using MarketplaceArtesanato.API.Hubs;
using MarketplaceArtesanato.API.Mapping;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Hubs;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Settings;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Data.Seed;
using MarketplaceArtesanato.Infrastructure.Consumers;
using MarketplaceArtesanato.Services;
using MarketplaceArtesanato.Services.Services;
using MarketplaceArtesanato.Services.Services.Configuration;
using MarketplaceArtesanato.Services.Services.Stripe;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Stripe;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddDbContext<ArtesianDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
// Configura Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AllowAdmin = false;
    configuration.ConnectTimeout = 5000;
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MarketplaceArtesanato:";
});
var secretKey = builder.Configuration["Turnstile:SecretKey"];
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MarketplaceArtesanato:";
});

builder.Services.AddAutoMapper(typeof(Program)); 
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOptions();
builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddScoped<IStorageService, BlobService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
builder.Services.AddScoped<IStripeConnectService, StripeConnectService>();
builder.Services.AddScoped<ISellerSubscriptionService, SellerSubscribeService>();
builder.Services.AddScoped<IPlatformFeeService, PlatformFeeService>();
builder.Services.AddScoped<ICommissionCalculationService, CommissionCalculationService>();
builder.Services.AddScoped<ICouponService, MarketplaceArtesanato.Services.Services.CouponService>();
builder.Services.AddScoped<ICouponAnalyticsService, MarketplaceArtesanato.Services.Services.CouponAnalyticsService>();
builder.Services.AddScoped<ICouponAutomationService, MarketplaceArtesanato.Services.Services.CouponAutomationService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ISellerAnalyticsService, SellerAnalyticsService>();
builder.Services.AddScoped<ISellerAnalyticsAdvancedService, SellerAnalyticsAdvancedService>();
builder.Services.AddPricingServices();
builder.Services.AddScoped<IProductService, MarketplaceArtesanato.Services.Services.ProductService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.Configure<AzureBlobSettings>(builder.Configuration.GetSection("Storage:AzureBlob"));

builder.Services.AddScoped<Stripe.BillingPortal.SessionService>();
builder.Services.AddScoped<Stripe.Checkout.SessionService>();

builder.Services.AddHttpClient<IShippingService, ShippingService>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SellerProPremium", policy =>
    {
        policy.Requirements.Add(new SellerPlanRequirement(SellerPlan.Pro));
    });
});

builder.Services.AddScoped<IAuthorizationHandler, SellerPlanRequirementHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",  
                "https://localhost:4200", 
                "http://localhost:3000"   
               )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});


builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CheckoutConsumer>();
    x.AddConsumer<PaymentConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration.GetConnectionString("RabbitMQ") ?? "localhost";

        cfg.Host(rabbitHost, "/", h => {
            h.Username("guest"); 
            h.Password("guest"); 
        });

        cfg.ReceiveEndpoint("checkout-queue", e =>
        {
            e.ConfigureConsumer<CheckoutConsumer>(context);
            e.ConcurrentMessageLimit = 8;
        });

        cfg.ReceiveEndpoint("payment-queue", e =>
        {
            e.ConfigureConsumer<PaymentConsumer>(context);
        });
    });
});


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MarketplaceArtesanato API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
});
builder.Services.Configure<TurnstileOptions>(
    builder.Configuration.GetSection("Turnstile")
);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketplaceArtesanato API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api/webhook"), appBuilder =>
{
    appBuilder.UseHttpsRedirection();
});

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationhub");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao executar seed do admin");
    }
}

app.Run();