using LaptopKDS.Api.Data;
using LaptopKDS.Api.DTOs;
using LaptopKDS.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopKDS.Api.Services;

/// <summary>
/// TOPSIS (Technique for Order of Preference by Similarity to Ideal Solution)
/// algoritmasını uygulayan servis.
///
/// Kriterler ve yönleri:
///   1. Price          → Maliyet (minimize) — düşük fiyat daha iyi
///   2. CpuScore       → Fayda  (maximize)
///   3. GpuScore       → Fayda  (maximize)
///   4. RamCapacityGB  → Fayda  (maximize)
///   5. WeightKG       → Maliyet (minimize) — hafiflik daha iyi
///   6. BatteryLifeHours → Fayda (maximize)
/// </summary>
public class TopsisService
{
    private readonly AppDbContext _db;
    private const int MaxResults = 40;
    private const double MinCompatibilityThreshold = 0.50; // %50 baraj filtresi (Pozitif ideale daha yakın olanlar)

    // Kriter indeksleri
    private const int IDX_PRICE   = 0;
    private const int IDX_CPU     = 1;
    private const int IDX_GPU     = 2;
    private const int IDX_RAM     = 3;
    private const int IDX_WEIGHT  = 4;
    private const int IDX_BATTERY = 5;
    private const int NUM_CRITERIA = 6;

    // true = Maliyet kriteri (küçük = daha iyi), false = Fayda kriteri (büyük = daha iyi)
    private static readonly bool[] IsCostCriterion = [true, false, false, false, true, false];

    public TopsisService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LaptopRecommendationDto>> GetRecommendationsAsync(
        RecommendationRequestDto request)
    {
        // 1) Bütçeye göre aktif laptop listesini filtrele
        var laptops = await _db.Laptops
            .Where(l => l.IsActive && l.Price <= request.BudgetMax)
            .OrderBy(l => l.Id)
            .ToListAsync();

        if (laptops.Count == 0)
            return [];

        // 2) Profil kriter ağırlıklarını getir
        var weightRow = await _db.ProfileCriterionWeights
            .FirstOrDefaultAsync(w => w.ProfileId == request.ProfileId);

        if (weightRow is null)
            throw new InvalidOperationException($"Profil ID {request.ProfileId} için ağırlık bulunamadı.");

        double[] weights = BuildWeightVector(weightRow);

        // 3) Karar matrisini oluştur (n×6)
        double[,] matrix = BuildDecisionMatrix(laptops);

        int n = laptops.Count; // satır (alternatif) sayısı

        // ─────────────────────────────────────────────────────────────
        // ADIM 1 — Vektör Normalizasyonu
        // R_ij = x_ij / sqrt(Σ x_ij²)
        // ─────────────────────────────────────────────────────────────
        double[,] R = NormalizeMatrix(matrix, n);

        // ─────────────────────────────────────────────────────────────
        // ADIM 2 — Ağırlıklı Normalize Matris
        // V_ij = R_ij × w_j
        // ─────────────────────────────────────────────────────────────
        double[,] V = WeightMatrix(R, weights, n);

        // ─────────────────────────────────────────────────────────────
        // ADIM 3 — Pozitif İdeal (A*) ve Negatif İdeal (A⁻) Çözümler
        //   Fayda kriteri : A*_j = max(V_ij),  A⁻_j = min(V_ij)
        //   Maliyet kriteri: A*_j = min(V_ij), A⁻_j = max(V_ij)
        // ─────────────────────────────────────────────────────────────
        double[] Apos = new double[NUM_CRITERIA]; // Pozitif ideal
        double[] Aneg = new double[NUM_CRITERIA]; // Negatif ideal

        ComputeIdealSolutions(V, n, out Apos, out Aneg);

        // ─────────────────────────────────────────────────────────────
        // ADIM 4 — Öklid Uzaklıkları
        //   S*_i = sqrt(Σ (V_ij - A*_j)²)
        //   S⁻_i = sqrt(Σ (V_ij - A⁻_j)²)
        // ─────────────────────────────────────────────────────────────
        double[] Spos = new double[n]; // A*'ya uzaklık
        double[] Sneg = new double[n]; // A⁻'ya uzaklık

        for (int i = 0; i < n; i++)
        {
            double sumPos = 0, sumNeg = 0;
            for (int j = 0; j < NUM_CRITERIA; j++)
            {
                double diffPos = V[i, j] - Apos[j];
                double diffNeg = V[i, j] - Aneg[j];
                sumPos += diffPos * diffPos;
                sumNeg += diffNeg * diffNeg;
            }
            Spos[i] = Math.Sqrt(sumPos);
            Sneg[i] = Math.Sqrt(sumNeg);
        }

        // ─────────────────────────────────────────────────────────────
        // ADIM 5 — Göreli Yakınlık Skoru (C*_i)
        //   C*_i = S⁻_i / (S*_i + S⁻_i)
        //   0 ≤ C*_i ≤ 1  →  1 en iyi
        // ─────────────────────────────────────────────────────────────
        double[] Cstar = new double[n];
        for (int i = 0; i < n; i++)
        {
            double denom = Spos[i] + Sneg[i];
            Cstar[i] = denom == 0 ? 0 : Sneg[i] / denom;
        }

        // ─────────────────────────────────────────────────────────────
        // ADIM 6 — TOPSIS Skoruna Göre Sıralama ve En İyi Alternatifler
        // ─────────────────────────────────────────────────────────────
        var profile = await _db.DecisionProfiles.FindAsync(request.ProfileId);
        string profileName = profile?.Name ?? "Kullanıcı";

        var results = laptops
            .Select((laptop, i) => new
            {
                Laptop = laptop,
                Score = Cstar[i]
            })
            .OrderByDescending(x => x.Score)
            .Take(MaxResults)
            .Select(x => new LaptopRecommendationDto(
                Id:                 x.Laptop.Id,
                Brand:              x.Laptop.Brand,
                Model:              x.Laptop.Model,
                Price:              x.Laptop.Price,
                CpuScore:           x.Laptop.CpuScore,
                GpuScore:           x.Laptop.GpuScore,
                RamCapacityGB:      x.Laptop.RamCapacityGB,
                WeightKG:           x.Laptop.WeightKG,
                BatteryLifeHours:   x.Laptop.BatteryLifeHours,
                SourceUrl:          x.Laptop.SourceUrl,
                CompatibilityScore: Math.Round(x.Score, 4),
                CompatibilityPercent: Math.Round(x.Score * 100, 1),
                ExplanationText:    GenerateExplanation(x.Laptop, x.Score, profileName, weightRow)
            ))
            .ToList();

        return results;
    }

    // ──────────────────────────────────────────────────────────────────
    // YARDIMCI METOTLAR
    // ──────────────────────────────────────────────────────────────────

    private static double[] BuildWeightVector(ProfileCriterionWeight w) =>
    [
        w.WeightPrice,
        w.WeightCpuScore,
        w.WeightGpuScore,
        w.WeightRamCapacity,
        w.WeightKG,
        w.WeightBattery
    ];

    private static double[,] BuildDecisionMatrix(List<Laptop> laptops)
    {
        int n = laptops.Count;
        var m = new double[n, NUM_CRITERIA];
        for (int i = 0; i < n; i++)
        {
            m[i, IDX_PRICE]   = (double)laptops[i].Price;
            m[i, IDX_CPU]     = laptops[i].CpuScore;
            m[i, IDX_GPU]     = laptops[i].GpuScore;
            m[i, IDX_RAM]     = laptops[i].RamCapacityGB;
            m[i, IDX_WEIGHT]  = laptops[i].WeightKG;
            m[i, IDX_BATTERY] = laptops[i].BatteryLifeHours;
        }
        return m;
    }

    /// <summary>Vektör normalizasyonu: R_ij = x_ij / sqrt(Σ x_kj²)</summary>
    private static double[,] NormalizeMatrix(double[,] matrix, int n)
    {
        var R = new double[n, NUM_CRITERIA];

        // Her kriter için sütun normu hesapla
        double[] colNorm = new double[NUM_CRITERIA];
        for (int j = 0; j < NUM_CRITERIA; j++)
        {
            double sumSq = 0;
            for (int i = 0; i < n; i++)
                sumSq += matrix[i, j] * matrix[i, j];
            colNorm[j] = Math.Sqrt(sumSq);
        }

        // Normalize et
        for (int i = 0; i < n; i++)
            for (int j = 0; j < NUM_CRITERIA; j++)
                R[i, j] = colNorm[j] == 0 ? 0 : matrix[i, j] / colNorm[j];

        return R;
    }

    /// <summary>Ağırlıklı normalize matris: V_ij = R_ij × w_j</summary>
    private static double[,] WeightMatrix(double[,] R, double[] weights, int n)
    {
        var V = new double[n, NUM_CRITERIA];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < NUM_CRITERIA; j++)
                V[i, j] = R[i, j] * weights[j];
        return V;
    }

    /// <summary>
    /// İdeal çözümleri hesaplar.
    /// Fayda  → Apos = max, Aneg = min
    /// Maliyet → Apos = min, Aneg = max
    /// </summary>
    private static void ComputeIdealSolutions(
        double[,] V, int n,
        out double[] Apos, out double[] Aneg)
    {
        Apos = new double[NUM_CRITERIA];
        Aneg = new double[NUM_CRITERIA];

        for (int j = 0; j < NUM_CRITERIA; j++)
        {
            double colMin = double.MaxValue, colMax = double.MinValue;
            for (int i = 0; i < n; i++)
            {
                if (V[i, j] < colMin) colMin = V[i, j];
                if (V[i, j] > colMax) colMax = V[i, j];
            }

            if (IsCostCriterion[j])
            {
                Apos[j] = colMin; // Maliyet: küçük = ideal
                Aneg[j] = colMax; // Maliyet: büyük = anti-ideal
            }
            else
            {
                Apos[j] = colMax; // Fayda: büyük = ideal
                Aneg[j] = colMin; // Fayda: küçük = anti-ideal
            }
        }
    }

    /// <summary>
    /// Profile ve skora göre dinamik Türkçe açıklama metni üretir.
    /// </summary>
    private static string GenerateExplanation(
        Laptop laptop,
        double score,
        string profileName,
        ProfileCriterionWeight w)
    {
        string scoreLabel = score switch
        {
            >= 0.90 => "mükemmel",
            >= 0.80 => "çok yüksek",
            >= 0.70 => "yüksek",
            _       => "uygun"
        };

        // Profilin en güçlü 2 kriterini belirle
        var criteriaDescriptions = new (double Weight, string Label)[]
        {
            (w.WeightPrice,       "bütçe-uyumluluğu"),
            (w.WeightCpuScore,    "CPU performansı"),
            (w.WeightGpuScore,    "GPU gücü"),
            (w.WeightRamCapacity, "RAM kapasitesi"),
            (w.WeightKG,          "taşınabilirliği"),
            (w.WeightBattery,     "pil ömrü"),
        };

        var topCriteria = criteriaDescriptions
            .OrderByDescending(c => c.Weight)
            .Take(2)
            .Select(c => c.Label)
            .ToArray();

        return $"Seçtiğiniz \"{profileName}\" profiline göre bu cihaz; " +
               $"{topCriteria[0]} ve {topCriteria[1]} açısından {scoreLabel} uyum " +
               $"(%{score * 100:F1}) sağlamaktadır. " +
               $"{laptop.Brand} {laptop.Model}, " +
               $"{laptop.RamCapacityGB}GB RAM, " +
               $"{laptop.BatteryLifeHours} saat pil ömrü ve " +
               $"{laptop.WeightKG:F2}kg ağırlığıyla bütçeniz dahilinde öne çıkmaktadır.";
    }
}
