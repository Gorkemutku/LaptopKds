namespace LaptopKDS.Api.Models;

/// <summary>
/// Her profil için AHP ile normalize edilmiş kriter ağırlıkları.
/// Tüm ağırlıkların toplamı = 1.0 olmalıdır.
/// </summary>
public class ProfileCriterionWeight
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public DecisionProfile Profile { get; set; } = null!;

    /// <summary>Fiyat ağırlığı (Maliyet — düşük fiyat tercih edilir)</summary>
    public double WeightPrice { get; set; }

    /// <summary>CPU skoru ağırlığı</summary>
    public double WeightCpuScore { get; set; }

    /// <summary>GPU skoru ağırlığı</summary>
    public double WeightGpuScore { get; set; }

    /// <summary>RAM kapasitesi ağırlığı</summary>
    public double WeightRamCapacity { get; set; }

    /// <summary>Ağırlık (kg) ağırlığı (Maliyet — hafif tercih edilir)</summary>
    public double WeightKG { get; set; }

    /// <summary>Pil ömrü ağırlığı</summary>
    public double WeightBattery { get; set; }
}
