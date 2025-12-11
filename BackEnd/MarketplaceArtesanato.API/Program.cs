using MarketplaceArtesanato.API.Hubs;
using MarketplaceArtesanato.API.Mapping;
using MarketplaceArtesanato.Application.Services;
using MarketplaceArtesanato.Core.Hubs;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Settings;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Data.Seed;
using MarketplaceArtesanato.Infrastructure.Consumers;
using MarketplaceArtesanato.Services;
using MarketplaceArtesanato.Services.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Stripe;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAutoMapper(typeof(ProductProfile));

builder.Services.AddScoped<IStorageService, BlobService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFavoritesService, FavoritesService>();
builder.Services.AddScoped<StripePaymentService>();
builder.Services.AddScoped<Stripe.BillingPortal.SessionService>();
builder.Services.AddScoped<Stripe.Checkout.SessionService>();
builder.Services.AddHttpClient<IShippingService, ShippingService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();


var stripeKey = builder.Configuration["Stripe:SecretKey"];
StripeConfiguration.ApiKey = stripeKey;

builder.Services.AddDbContext<ArtesianDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddControllers();
builder.Services.Configure<AzureBlobSettings>(builder.Configuration.GetSection("Storage:AzureBlob"));
builder.Services.AddSignalR();

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
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Configuração para Upload de Arquivo no Swagger
    c.SwaggerDoc("v1", new()
    {
        Title = "MarketplaceArtesanato API",
        Version = "v1"
    });
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",    
                "http://localhost:3000"     
               )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });

    options.AddPolicy("ProdCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "https://seusite.com.br",
                "https://www.seusite.com.br"
               )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",           
                "https://localhost:4200",          
                "http://localhost:3000"           
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); 
    });
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MarketplaceArtesanato:";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";

    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AllowAdmin = false;
    configuration.ConnectTimeout = 5000;
    configuration.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddAuthorization();
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.AddSignalR();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CheckoutConsumer>();
    x.AddConsumer<PaymentConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ") ?? "localhost");

        cfg.ReceiveEndpoint("checkout-queue", e =>
        {
            e.ConfigureConsumer<CheckoutConsumer>(context);
            e.ConcurrentMessageLimit = 8;
        });
    });
});
builder.Services.AddDbContext<ArtesianDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )));

var app = builder.Build();

app.UseCors(app.Environment.IsDevelopment() ? "DevCorsPolicy" : "ProdCorsPolicy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationhub"); 

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketplaceArtesanato API V1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});
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
app.UseHttpsRedirection();
app.MapControllers();
app.UseAuthorization();

app.Run();