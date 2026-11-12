namespace LaptopKDS.Api.Models;

public class Laptop
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    /// <summary>Fiyat — Maliyet kriteri (minimize)</summary>
    public decimal Price { get; set; }

    /// <summary>CPU Cinebench/Passmark skoru — Fayda kriteri (maximize)</summary>
    public double CpuScore { get; set; }

    /// <summary>GPU 3DMark skoru — Fayda kriteri (maximize)</summary>
    public double GpuScore { get; set; }

    /// <summary>RAM kapasitesi (GB) — Fayda kriteri (maximize)</summary>
    public double RamCapacityGB { get; set; }

    /// <summary>Ağırlık (kg) — Maliyet kriteri (minimize)</summary>
    public double WeightKG { get; set; }

    /// <summary>Pil ömrü (saat) — Fayda kriteri (maximize)</summary>
    public double BatteryLifeHours { get; set; }

    /// <summary>Akakçe veya Epey ürün linki</summary>
    public string SourceUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
