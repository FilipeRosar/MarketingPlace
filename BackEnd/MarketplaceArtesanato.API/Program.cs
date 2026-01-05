using MarketplaceArtesanato.API.Hubs;
using MarketplaceArtesanato.API.Mapping;
using MarketplaceArtesanato.Core.Hubs;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Settings;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Data.Seed;
using MarketplaceArtesanato.Infrastructure.Consumers;
using MarketplaceArtesanato.Services;
using MarketplaceArtesanato.Services.Services;
using MarketplaceArtesanato.Services.Services.Stripe;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Stripe;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAÇÃO DE AMBIENTE E BANCO
// ==========================================
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configura Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Configura Banco de Dados (SQL Server)
builder.Services.AddDbContext<ArtesianDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null));
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

// ==========================================
// 2. SERVIÇOS DA APLICAÇÃO (DI)
// ==========================================
builder.Services.AddAutoMapper(typeof(Program)); // Escaneia perfis
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// Controllers com Enum Converter
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Injeção de Dependência dos Serviços
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
builder.Services.AddScoped<IProductService, MarketplaceArtesanato.Services.Services.ProductService>();
builder.Services.Configure<AzureBlobSettings>(builder.Configuration.GetSection("Storage:AzureBlob"));

// Serviços do Stripe SDK
builder.Services.AddScoped<Stripe.BillingPortal.SessionService>();
builder.Services.AddScoped<Stripe.Checkout.SessionService>();

// HttpClient para Shipping
builder.Services.AddHttpClient<IShippingService, ShippingService>();

// ==========================================
// 3. AUTHENTICATION & JWT
// ==========================================
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

builder.Services.AddAuthorization();

// ==========================================
// 4. CORS (CONFIGURAÇÃO CORRIGIDA)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",  // Angular HTTP
                "https://localhost:4200", // Angular HTTPS
                "http://localhost:3000"   // React/Next (caso use)
               )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});

// ==========================================
// 5. MASSTRANSIT (RABBITMQ)
// ==========================================
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

// ==========================================
// 6. SWAGGER
// ==========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MarketplaceArtesanato API", Version = "v1" });

    // Auth no Swagger
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

var app = builder.Build();

// ==========================================
// 7. PIPELINE DE REQUISIÇÃO (MIDDLEWARE)
// ==========================================

// Swagger (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketplaceArtesanato API V1");
        c.RoutePrefix = "swagger";
    });
    // app.MapOpenApi(); // Removido pois já usamos SwaggerGen acima, evita conflito
}

app.UseHttpsRedirection();

// 1. CORS deve vir ANTES de Auth e ANTES dos Controllers
app.UseCors("AllowAngular");

// 2. Autenticação deve vir ANTES de Autorização
app.UseAuthentication();
app.UseAuthorization();

// 3. Hubs e Controllers
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationhub");
app.MapControllers();

// 4. Seed Data
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