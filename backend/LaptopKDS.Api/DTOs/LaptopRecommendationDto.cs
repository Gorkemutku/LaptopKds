namespace LaptopKDS.Api.DTOs;

public record LaptopRecommendationDto(
    int Id,
    string Brand,
    string Model,
    decimal Price,
    double CpuScore,
    double GpuScore,
    double RamCapacityGB,
    double WeightKG,
    double BatteryLifeHours,
    string SourceUrl,
    /// <summary>TOPSIS göreli yakınlık skoru (0–1 arası, 1 en iyi)</summary>
    double CompatibilityScore,
    /// <summary>Yüzde olarak uyum skoru (örn. 84.5)</summary>
    double CompatibilityPercent,
    /// <summary>Sistem tarafından üretilen açıklama metni</summary>
    string ExplanationText
);
