using LaptopKDS.Api.Data;
using LaptopKDS.Api.Services;
using LaptopKDS.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Servisler ───────────────────────────────────────────────────────────────

// EF Core + SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=laptopkds.db";
var dbPathMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
if (dbPathMatch.Success)
{
    var rawFilePath = dbPathMatch.Groups[1].Value.Trim();
    var dir = Path.GetDirectoryName(rawFilePath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }
}
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// TOPSIS servisi
builder.Services.AddScoped<TopsisService>();

// Web Scraper (arka plan servisi)
builder.Services.AddHostedService<ScraperBackgroundService>();

// Controllers
builder.Services.AddControllers();

// .NET 10 built-in OpenAPI (Swagger UI: /openapi/v1.json)
builder.Services.AddOpenApi();

// CORS — Geliştirme ve Docker ortamları için esnek izinler
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ─── Uygulama ────────────────────────────────────────────────────────────────

var app = builder.Build();

// Veritabanını oluştur + seed verisi yükle
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
}

app.UseCors("AllowFrontend");
app.MapControllers();

// Sağlık kontrolü
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.UtcNow }));

app.Run();
