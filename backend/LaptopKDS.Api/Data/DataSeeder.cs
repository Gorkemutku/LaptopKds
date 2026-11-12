using LaptopKDS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopKDS.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Zaten seed yapıldıysa geç
        if (await db.DecisionProfiles.AnyAsync()) return;

        // ========== KARAR PROFİLLERİ ==========
        var profiles = new List<DecisionProfile>
        {
            new() { Id = 1, Name = "Yazılımcı",  IconEmoji = "👨‍💻", Description = "Derleme, IDE, sanal makine ve çoklu görev için yüksek CPU ve RAM performansı önceliklidir." },
            new() { Id = 2, Name = "Oyuncu",      IconEmoji = "🎮", Description = "Yüksek GPU performansı, hızlı RAM ve tercihen hafif bir yapı aranır." },
            new() { Id = 3, Name = "Öğrenci",     IconEmoji = "🎓", Description = "Uygun fiyat, uzun pil ömrü ve taşınabilirlik ön plandadır." },
            new() { Id = 4, Name = "Tasarımcı",   IconEmoji = "🎨", Description = "Yüksek GPU, yeterli RAM ve renk doğruluğu için dengeli bir sistem tercih edilir." },
        };
        await db.DecisionProfiles.AddRangeAsync(profiles);

        // ========== KRİTER AĞIRLIKLARI (AHP normalize) ==========
        // Toplam = 1.0  |  Price  Cpu   Gpu   Ram   Kg    Battery
        var weights = new List<ProfileCriterionWeight>
        {
            // Yazılımcı: CPU ve RAM ağırlığı yüksek, GPU düşük, pil orta
            new() { ProfileId = 1, WeightPrice = 0.10, WeightCpuScore = 0.35, WeightGpuScore = 0.10, WeightRamCapacity = 0.25, WeightKG = 0.08, WeightBattery = 0.12 },
            // Oyuncu: GPU ve CPU ağırlığı yüksek, pil düşük
            new() { ProfileId = 2, WeightPrice = 0.10, WeightCpuScore = 0.25, WeightGpuScore = 0.40, WeightRamCapacity = 0.15, WeightKG = 0.05, WeightBattery = 0.05 },
            // Öğrenci: Fiyat ve pil ön planda, performans ikincil
            new() { ProfileId = 3, WeightPrice = 0.30, WeightCpuScore = 0.15, WeightGpuScore = 0.05, WeightRamCapacity = 0.15, WeightKG = 0.15, WeightBattery = 0.20 },
            // Tasarımcı: GPU, CPU ve RAM dengeli, fiyat orta
            new() { ProfileId = 4, WeightPrice = 0.12, WeightCpuScore = 0.22, WeightGpuScore = 0.28, WeightRamCapacity = 0.22, WeightKG = 0.08, WeightBattery = 0.08 },
        };
        await db.ProfileCriterionWeights.AddRangeAsync(weights);

        // ========== LAPTOP VERİLERİ ==========
        // CpuScore: Cinebench R23 Multi-Core benzeri normalize skor (0–10000 aralığı)
        // GpuScore: 3DMark TimeSpy benzeri normalize skor (0–15000 aralığı)
        var laptops = new List<Laptop>
        {
            // === YÜKSEK SEGMENT ===
            new() { Brand = "Apple",   Model = "MacBook Pro 14\" M4 Pro",          Price = 89990,  CpuScore = 9500,  GpuScore = 7200,  RamCapacityGB = 24, WeightKG = 1.55, BatteryLifeHours = 18, SourceUrl = "https://www.akakce.com/apple-macbook-pro-14/",           IsActive = true },
            new() { Brand = "Apple",   Model = "MacBook Pro 16\" M4 Max",          Price = 139990, CpuScore = 9900,  GpuScore = 11000, RamCapacityGB = 48, WeightKG = 2.14, BatteryLifeHours = 22, SourceUrl = "https://www.akakce.com/apple-macbook-pro-16/",           IsActive = true },
            new() { Brand = "ASUS",    Model = "ROG Zephyrus G16 RTX 4090",        Price = 115000, CpuScore = 8800,  GpuScore = 14500, RamCapacityGB = 32, WeightKG = 2.30, BatteryLifeHours = 8,  SourceUrl = "https://www.akakce.com/asus-rog-zephyrus-g16/",          IsActive = true },
            new() { Brand = "ASUS",    Model = "ProArt Studiobook 16 RTX 4070",    Price = 85000,  CpuScore = 8600,  GpuScore = 10500, RamCapacityGB = 32, WeightKG = 2.40, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/asus-proart-studiobook/",          IsActive = true },
            new() { Brand = "Lenovo",  Model = "ThinkPad X1 Carbon Gen 12",        Price = 72000,  CpuScore = 8200,  GpuScore = 2500,  RamCapacityGB = 32, WeightKG = 1.12, BatteryLifeHours = 15, SourceUrl = "https://www.akakce.com/lenovo-thinkpad-x1-carbon/",      IsActive = true },
            new() { Brand = "Dell",    Model = "XPS 15 RTX 4060 i9",               Price = 95000,  CpuScore = 8700,  GpuScore = 8200,  RamCapacityGB = 32, WeightKG = 1.86, BatteryLifeHours = 10, SourceUrl = "https://www.akakce.com/dell-xps-15/",                   IsActive = true },
            new() { Brand = "HP",      Model = "Spectre x360 14 OLED",             Price = 55000,  CpuScore = 7500,  GpuScore = 3200,  RamCapacityGB = 16, WeightKG = 1.40, BatteryLifeHours = 14, SourceUrl = "https://www.akakce.com/hp-spectre-x360/",               IsActive = true },
            new() { Brand = "MSI",     Model = "Titan GT77 HX RTX 4090",           Price = 130000, CpuScore = 9200,  GpuScore = 14800, RamCapacityGB = 64, WeightKG = 3.30, BatteryLifeHours = 5,  SourceUrl = "https://www.akakce.com/msi-titan-gt77/",                IsActive = true },
            new() { Brand = "Razer",   Model = "Blade 16 RTX 4080",                Price = 110000, CpuScore = 8900,  GpuScore = 13000, RamCapacityGB = 32, WeightKG = 2.19, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/razer-blade-16/",                IsActive = true },
            new() { Brand = "Lenovo",  Model = "Legion Pro 7i Gen 9 RTX 4080",     Price = 90000,  CpuScore = 9000,  GpuScore = 12800, RamCapacityGB = 32, WeightKG = 2.60, BatteryLifeHours = 6,  SourceUrl = "https://www.akakce.com/lenovo-legion-pro-7i/",          IsActive = true },

            // === ORTA-ÜST SEGMENT ===
            new() { Brand = "Apple",   Model = "MacBook Pro 14\" M4",              Price = 59990,  CpuScore = 8800,  GpuScore = 5200,  RamCapacityGB = 16, WeightKG = 1.55, BatteryLifeHours = 17, SourceUrl = "https://www.akakce.com/apple-macbook-pro-14-m4/",        IsActive = true },
            new() { Brand = "ASUS",    Model = "ROG Strix G16 RTX 4070",           Price = 65000,  CpuScore = 8400,  GpuScore = 10800, RamCapacityGB = 16, WeightKG = 2.50, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/asus-rog-strix-g16/",            IsActive = true },
            new() { Brand = "Dell",    Model = "Inspiron 16 Plus RTX 4060",        Price = 42000,  CpuScore = 7800,  GpuScore = 7500,  RamCapacityGB = 16, WeightKG = 2.00, BatteryLifeHours = 9,  SourceUrl = "https://www.akakce.com/dell-inspiron-16-plus/",          IsActive = true },
            new() { Brand = "HP",      Model = "OMEN 16 RTX 4070",                 Price = 55000,  CpuScore = 8000,  GpuScore = 9800,  RamCapacityGB = 16, WeightKG = 2.35, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/hp-omen-16/",                    IsActive = true },
            new() { Brand = "Lenovo",  Model = "IdeaPad Pro 5 RTX 4060",           Price = 38000,  CpuScore = 7600,  GpuScore = 7200,  RamCapacityGB = 16, WeightKG = 1.78, BatteryLifeHours = 11, SourceUrl = "https://www.akakce.com/lenovo-ideapad-pro-5/",          IsActive = true },
            new() { Brand = "MSI",     Model = "Creator Z16 HX Studio RTX 4070",   Price = 68000,  CpuScore = 8300,  GpuScore = 9200,  RamCapacityGB = 32, WeightKG = 2.10, BatteryLifeHours = 8,  SourceUrl = "https://www.akakce.com/msi-creator-z16/",               IsActive = true },
            new() { Brand = "ASUS",    Model = "Zenbook Pro 16X OLED RTX 4060",    Price = 72000,  CpuScore = 8100,  GpuScore = 7800,  RamCapacityGB = 32, WeightKG = 2.40, BatteryLifeHours = 9,  SourceUrl = "https://www.akakce.com/asus-zenbook-pro-16x/",          IsActive = true },
            new() { Brand = "HP",      Model = "EliteBook 860 G11 AI",             Price = 48000,  CpuScore = 7900,  GpuScore = 2800,  RamCapacityGB = 32, WeightKG = 1.70, BatteryLifeHours = 14, SourceUrl = "https://www.akakce.com/hp-elitebook-860/",              IsActive = true },
            new() { Brand = "Lenovo",  Model = "ThinkPad T14s Gen 5 AMD",          Price = 32000,  CpuScore = 7200,  GpuScore = 2200,  RamCapacityGB = 16, WeightKG = 1.27, BatteryLifeHours = 16, SourceUrl = "https://www.akakce.com/lenovo-thinkpad-t14s/",          IsActive = true },
            new() { Brand = "Microsoft",Model = "Surface Laptop 5 i7",             Price = 45000,  CpuScore = 7400,  GpuScore = 2600,  RamCapacityGB = 16, WeightKG = 1.50, BatteryLifeHours = 13, SourceUrl = "https://www.akakce.com/microsoft-surface-laptop-5/",    IsActive = true },

            // === ORTA SEGMENT ===
            new() { Brand = "ASUS",    Model = "VivoBook Pro 15 OLED RTX 4060",    Price = 28000,  CpuScore = 7000,  GpuScore = 7000,  RamCapacityGB = 16, WeightKG = 1.65, BatteryLifeHours = 8,  SourceUrl = "https://www.akakce.com/asus-vivobook-pro-15/",          IsActive = true },
            new() { Brand = "Lenovo",  Model = "Legion 5i Gen 9 RTX 4060",         Price = 32000,  CpuScore = 7800,  GpuScore = 8500,  RamCapacityGB = 16, WeightKG = 2.40, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/lenovo-legion-5i-gen-9/",        IsActive = true },
            new() { Brand = "HP",      Model = "Pavilion Gaming 16 RTX 4060",      Price = 29000,  CpuScore = 7300,  GpuScore = 7600,  RamCapacityGB = 16, WeightKG = 2.30, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/hp-pavilion-gaming-16/",         IsActive = true },
            new() { Brand = "Acer",    Model = "Predator Helios Neo 16 RTX 4070",  Price = 48000,  CpuScore = 8000,  GpuScore = 10200, RamCapacityGB = 16, WeightKG = 2.70, BatteryLifeHours = 6,  SourceUrl = "https://www.akakce.com/acer-predator-helios-neo-16/",   IsActive = true },
            new() { Brand = "Acer",    Model = "Swift X 16 RTX 4070",              Price = 38000,  CpuScore = 7600,  GpuScore = 8800,  RamCapacityGB = 16, WeightKG = 1.85, BatteryLifeHours = 10, SourceUrl = "https://www.akakce.com/acer-swift-x-16/",              IsActive = true },
            new() { Brand = "MSI",     Model = "Cyborg 15 RTX 4060",               Price = 25000,  CpuScore = 7100,  GpuScore = 7800,  RamCapacityGB = 16, WeightKG = 2.00, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/msi-cyborg-15/",                 IsActive = true },
            new() { Brand = "ASUS",    Model = "TUF Gaming A15 RTX 4060",          Price = 24000,  CpuScore = 7200,  GpuScore = 8200,  RamCapacityGB = 16, WeightKG = 2.30, BatteryLifeHours = 8,  SourceUrl = "https://www.akakce.com/asus-tuf-gaming-a15/",           IsActive = true },
            new() { Brand = "Dell",    Model = "G15 5530 RTX 4060",                Price = 27000,  CpuScore = 7400,  GpuScore = 7900,  RamCapacityGB = 16, WeightKG = 2.80, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/dell-g15-5530/",                 IsActive = true },

            // === GİRİŞ SEGMENT ===
            new() { Brand = "HP",      Model = "15s i5-1335U",                     Price = 12000,  CpuScore = 5500,  GpuScore = 1200,  RamCapacityGB = 8,  WeightKG = 1.69, BatteryLifeHours = 8,  SourceUrl = "https://www.akakce.com/hp-15s-i5/",                     IsActive = true },
            new() { Brand = "Lenovo",  Model = "IdeaPad Slim 3 Ryzen 5 7520U",     Price = 10500,  CpuScore = 5200,  GpuScore = 900,   RamCapacityGB = 8,  WeightKG = 1.60, BatteryLifeHours = 9,  SourceUrl = "https://www.akakce.com/lenovo-ideapad-slim-3/",         IsActive = true },
            new() { Brand = "Acer",    Model = "Aspire 5 i5-1335U",                Price = 11000,  CpuScore = 5300,  GpuScore = 1100,  RamCapacityGB = 8,  WeightKG = 1.70, BatteryLifeHours = 9,  SourceUrl = "https://www.akakce.com/acer-aspire-5/",                 IsActive = true },
            new() { Brand = "ASUS",    Model = "VivoBook 15 X1504 i3-1215U",       Price = 8500,   CpuScore = 4200,  GpuScore = 700,   RamCapacityGB = 8,  WeightKG = 1.70, BatteryLifeHours = 8,  SourceUrl = "https://www.akakce.com/asus-vivobook-15/",              IsActive = true },
            new() { Brand = "HP",      Model = "250 G10 i5-1335U 16GB",            Price = 14000,  CpuScore = 5600,  GpuScore = 1300,  RamCapacityGB = 16, WeightKG = 1.74, BatteryLifeHours = 10, SourceUrl = "https://www.akakce.com/hp-250-g10/",                    IsActive = true },
            new() { Brand = "Lenovo",  Model = "IdeaPad Gaming 3 RTX 3050",        Price = 18000,  CpuScore = 6500,  GpuScore = 4500,  RamCapacityGB = 16, WeightKG = 2.20, BatteryLifeHours = 6,  SourceUrl = "https://www.akakce.com/lenovo-ideapad-gaming-3/",       IsActive = true },
            new() { Brand = "ASUS",    Model = "TUF Gaming F15 RTX 3050",          Price = 16500,  CpuScore = 6200,  GpuScore = 4200,  RamCapacityGB = 8,  WeightKG = 2.20, BatteryLifeHours = 7,  SourceUrl = "https://www.akakce.com/asus-tuf-gaming-f15/",           IsActive = true },
            new() { Brand = "Apple",   Model = "MacBook Air M3 8GB",               Price = 42990,  CpuScore = 7800,  GpuScore = 3600,  RamCapacityGB = 8,  WeightKG = 1.24, BatteryLifeHours = 18, SourceUrl = "https://www.akakce.com/apple-macbook-air-m3/",           IsActive = true },
            new() { Brand = "Apple",   Model = "MacBook Air M3 16GB",              Price = 51990,  CpuScore = 7900,  GpuScore = 3800,  RamCapacityGB = 16, WeightKG = 1.24, BatteryLifeHours = 18, SourceUrl = "https://www.akakce.com/apple-macbook-air-m3-16gb/",      IsActive = true },
        };

        await db.Laptops.AddRangeAsync(laptops);
        await db.SaveChangesAsync();
    }
}
