using LaptopKDS.Api.Data;
using LaptopKDS.Api.Services;
using LaptopKDS.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Servisler ───────────────────────────────────────────────────────────────

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=laptopkds.db"));

// TOPSIS servisi
builder.Services.AddScoped<TopsisService>();

// Web Scraper (arka plan servisi)
builder.Services.AddHostedService<ScraperBackgroundService>();

// Controllers
builder.Services.AddControllers();

// .NET 10 built-in OpenAPI (Swagger UI: /openapi/v1.json)
builder.Services.AddOpenApi();

// CORS — React dev sunucusuna (5173) izin ver
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://laptopkds.example.com"
            )
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
