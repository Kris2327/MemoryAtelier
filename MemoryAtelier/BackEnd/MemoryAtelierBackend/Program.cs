using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MemoryAtelierBackend.Data;
using MemoryAtelierBackend.Services;
using MemoryAtelierBackend.DTOs;

// зарежда .env в реалните environment variables, преди конфигурацията да се построи от тях
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseUrl = new[]
{
    builder.Configuration.GetConnectionString("Default"),
    builder.Configuration["Supabase:DatabaseUrl"],
    builder.Configuration["DATABASE_URL"]
}.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException("Database connection string is not configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseUrl));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is not configured.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<SupabaseStorageService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<FavouritesService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<HeroImageService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ContactMessageService>();
builder.Services.AddHostedService<TrashCleanupService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<BankTransferSettings>(builder.Configuration.GetSection("BankTransfer"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.AddOutputCache(options =>
{
    // Кешира публичните GET заявки за каталога (продукти/категории) за кратко.
    // Не кешира заявки с Authorization хедър, за да не изтече скрито/админско съдържание към анонимни клиенти.
    options.AddPolicy("Catalog", policy => policy
        .Expire(TimeSpan.FromSeconds(60))
        .Tag("catalog")
        .With(ctx => !ctx.HttpContext.Request.Headers.ContainsKey("Authorization")));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Общ таван за всеки endpoint, който няма собствена по-строга политика — пази backend-а/базата
    // от внезапен наплив (легитимен пик или флууд), без да пречи на нормалното сърфиране в сайта
    // (една зареждаща се страница дърпа паралелно няколко endpoint-а).
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // 5 съобщения на 10 минути за всеки клиентски IP
    options.AddPolicy("contact", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));

    // По-строг лимит за вход/забравена-парола — основната цел за brute-force/bot атаки.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));

    // Регистрация на нови акаунти — 20 на минута за всеки клиентски IP.
    options.AddPolicy("register", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// Зад Cloudflare реалният IP на посетителя идва в CF-Connecting-IP, а RemoteIpAddress е IP на Cloudflare.
// Без това всички потребители делят един rate-limit кош. Хедърът се приема САМО от Cloudflare мрежи,
// иначе всеки би могъл да го фалшифицира и да заобиколи лимитите.
var cloudflareEnabled = builder.Configuration.GetValue<bool>("Cloudflare:Enabled");
if (cloudflareEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        options.ForwardedForHeaderName = "CF-Connecting-IP";
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();

        // https://www.cloudflare.com/ips/ — списъкът се променя рядко
        var cloudflareNetworks = new[]
        {
            "173.245.48.0/20", "103.21.244.0/22", "103.22.200.0/22", "103.31.4.0/22",
            "141.101.64.0/18", "108.162.192.0/18", "190.93.240.0/20", "188.114.96.0/20",
            "197.234.240.0/22", "198.41.128.0/17", "162.158.0.0/15", "104.16.0.0/13",
            "104.24.0.0/14", "172.64.0.0/13", "131.0.72.0/22",
            "2400:cb00::/32", "2606:4700::/32", "2803:f800::/32", "2405:b500::/32",
            "2405:8100::/32", "2a06:98c0::/29", "2c0f:f248::/32"
        };
        // допълнителни доверени мрежи, ако между Cloudflare и приложението има още един proxy (хостинг платформа)
        var extraNetworks = builder.Configuration.GetSection("Cloudflare:ExtraTrustedNetworks").Get<string[]>()
            ?? Array.Empty<string>();

        foreach (var cidr in cloudflareNetworks.Concat(extraNetworks))
        {
            var parts = cidr.Split('/');
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
                System.Net.IPAddress.Parse(parts[0]), int.Parse(parts[1])));
        }
    });
}

var app = builder.Build();

if (cloudflareEnabled)
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.MapControllers();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.Run();
