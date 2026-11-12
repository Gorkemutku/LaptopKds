using HtmlAgilityPack;
using LaptopKDS.Api.Data;
using LaptopKDS.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Web;

namespace LaptopKDS.Api.Workers;

/// <summary>
/// Akakçe, Epey vb. sitelerden laptop verilerini periyodik olarak çeken arka plan servisi.
/// 
/// Çözülen Sorunlar:
/// 1. Mutlak URL Dönüşümü: Göreceli (relative) veya protokolsüz linkler tam URL'e dönüştürülür.
/// 2. Kalıcı Detay Sayfası: Geçici oturum, arama/filtre veya yönlendirme (/c/?, /git/?) linkleri elenir,
///    yalnızca kalıcı ürün detay sayfaları (.html uzantılı slug/id) veritabanına kaydedilir.
/// 3. URL Doğrulama & Sanitization: Query parametrelerindeki tracking/session kodları (utm_*, gclid, ref vb.)
///    temizlenir ve URI şeması/host doğrulaması yapılır.
/// </summary>
public class ScraperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScraperBackgroundService> _logger;
    private readonly IConfiguration _config;

    // Scraping periyodu: varsayılan 6 saatte bir
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    // Temizlenecek geçici/takip parametreleri
    private static readonly HashSet<string> TrackingQueryParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content",
        "gclid", "fbclid", "ref", "reference", "session_id", "sid", "adjust_tracker",
        "ad_id", "campaign_id", "sort", "filtre", "page"
    };

    public ScraperBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScraperBackgroundService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Scraper] ScraperBackgroundService başlatıldı.");

        // Uygulamanın ve veritabanının tam hazır olması için başlangıç gecikmesi
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScrapeAndUpsertAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Scraper] Scraping döngüsünde beklenmeyen hata oluştu.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ScrapeAndUpsertAsync(CancellationToken ct)
    {
        _logger.LogInformation("[Scraper] Veri kazıma işlemi başlatılıyor: {Time}", DateTime.UtcNow);

        // Kazınacak hedef kategori sayfaları
        var sources = new List<ScrapeTarget>
        {
            new("https://www.akakce.com/laptop/", TargetSiteType.Akakce),
            new("https://www.akakce.com/oyun-bilgisayari/", TargetSiteType.Akakce),
            new("https://www.epey.com/laptop/", TargetSiteType.Epey),
            new("https://www.epey.com/laptop/oyuncu-laptop/", TargetSiteType.Epey)
        };

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var web = new HtmlWeb
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            Timeout = 20000,
            AutoDetectEncoding = true
        };

        int totalSavedOrUpdated = 0;

        foreach (var source in sources)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("[Scraper] URL taranıyor: {Url}", source.Url);
                var doc = await web.LoadFromWebAsync(source.Url, ct);

                var items = source.SiteType switch
                {
                    TargetSiteType.Akakce => ParseAkakceListings(doc, source.Url),
                    TargetSiteType.Epey   => ParseEpeyListings(doc, source.Url),
                    _ => new List<ScrapedLaptop>()
                };

                _logger.LogInformation("[Scraper] {Url} kaynağından {Count} adet geçerli ürün ayrıştırıldı.", source.Url, items.Count);

                foreach (var item in items)
                {
                    await UpsertLaptopAsync(db, item, ct);
                    totalSavedOrUpdated++;
                }

                await db.SaveChangesAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(3), ct); // Siteleri yormamak için nazik gecikme
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scraper] Kaynak işlenirken hata oluştu: {Url}", source.Url);
            }
        }

        _logger.LogInformation("[Scraper] Veri kazıma tamamlandı. Toplam işlenen ürün: {Total}", totalSavedOrUpdated);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // AKAKÇE PARSE MANTIĞI
    // ─────────────────────────────────────────────────────────────────────────────
    private List<ScrapedLaptop> ParseAkakceListings(HtmlDocument doc, string baseUrl)
    {
        var results = new List<ScrapedLaptop>();

        // Ürün listesindeki kart elemanları
        var nodes = doc.DocumentNode.SelectNodes("//ul[contains(@class,'product_list') or contains(@class,'resultlist')]//li");
        if (nodes == null || nodes.Count == 0)
        {
            nodes = doc.DocumentNode.SelectNodes("//li[contains(@data-pr,'')]");
        }

        if (nodes == null) return results;

        foreach (var node in nodes)
        {
            try
            {
                // Kalıcı linki barındıran başlık etiketi
                var linkNode = node.SelectSingleNode(".//h3/a")
                            ?? node.SelectSingleNode(".//a[contains(@href,'.html')]");

                if (linkNode == null) continue;

                string? rawName = linkNode.InnerText?.Trim();
                string? rawHref = linkNode.GetAttributeValue("href", null);

                if (string.IsNullOrWhiteSpace(rawName) || string.IsNullOrWhiteSpace(rawHref))
                    continue;

                // Fiyatı al
                var priceNode = node.SelectSingleNode(".//span[contains(@class,'pt_v8') or contains(@class,'price')]");
                decimal price = ParsePrice(priceNode?.InnerText);
                if (price <= 0) continue;

                // URL Sanitization & Mutlak URL Dönüşümü
                string? permanentUrl = SanitizeAndBuildAbsoluteUrl(rawHref, baseUrl);
                if (permanentUrl == null || !IsValidPermanentProductUrl(permanentUrl, TargetSiteType.Akakce))
                {
                    _logger.LogDebug("[Scraper] Geçersiz/geçici Akakçe linki atlandı: {RawHref}", rawHref);
                    continue;
                }

                results.Add(new ScrapedLaptop
                {
                    Brand = ExtractBrand(rawName),
                    Model = CleanModelName(rawName),
                    Price = price,
                    SourceUrl = permanentUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[Scraper] Akakçe kartı parse edilirken hata oluştu.");
            }
        }

        return results;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // EPEY PARSE MANTIĞI
    // ─────────────────────────────────────────────────────────────────────────────
    private List<ScrapedLaptop> ParseEpeyListings(HtmlDocument doc, string baseUrl)
    {
        var results = new List<ScrapedLaptop>();

        // Epey ürün kartları
        var nodes = doc.DocumentNode.SelectNodes("//ul[@id='liste']//li[contains(@class,'urun')]")
                 ?? doc.DocumentNode.SelectNodes("//li[contains(@class,'urun')]");

        if (nodes == null) return results;

        foreach (var node in nodes)
        {
            try
            {
                var linkNode = node.SelectSingleNode(".//div[contains(@class,'detay')]//a[contains(@class,'baslik')]")
                            ?? node.SelectSingleNode(".//a[contains(@class,'baslik')]")
                            ?? node.SelectSingleNode(".//a[contains(@href,'.html')]");

                if (linkNode == null) continue;

                string? rawName = linkNode.InnerText?.Trim();
                string? rawHref = linkNode.GetAttributeValue("href", null);

                if (string.IsNullOrWhiteSpace(rawName) || string.IsNullOrWhiteSpace(rawHref))
                    continue;

                // Fiyatı al
                var priceNode = node.SelectSingleNode(".//span[contains(@class,'fiyat')]//a")
                             ?? node.SelectSingleNode(".//span[contains(@class,'fiyat')]");
                decimal price = ParsePrice(priceNode?.InnerText);
                if (price <= 0) continue;

                // URL Sanitization & Mutlak URL Dönüşümü
                string? permanentUrl = SanitizeAndBuildAbsoluteUrl(rawHref, baseUrl);
                if (permanentUrl == null || !IsValidPermanentProductUrl(permanentUrl, TargetSiteType.Epey))
                {
                    _logger.LogDebug("[Scraper] Geçersiz/geçici Epey linki atlandı: {RawHref}", rawHref);
                    continue;
                }

                results.Add(new ScrapedLaptop
                {
                    Brand = ExtractBrand(rawName),
                    Model = CleanModelName(rawName),
                    Price = price,
                    SourceUrl = permanentUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[Scraper] Epey kartı parse edilirken hata oluştu.");
            }
        }

        return results;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 1. MUTLAK URL DÖNÜŞÜMÜ & 3. SANITIZATION (TEMİZLEME)
    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Gelen göreceli (relative) veya eksik linki ana domain ile birleştirerek
    /// mutlak (absolute) URL yapar ve geçici query parametrelerini temizler.
    /// </summary>
    public static string? SanitizeAndBuildAbsoluteUrl(string rawHref, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(rawHref)) return null;

        string trimmed = rawHref.Trim();

        // javascript:, mailto:, tel: gibi sahte linkleri ele
        if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("#"))
        {
            return null;
        }

        // Protokolsüz gelen linkleri tamamla: "//www.epey.com/laptop/..." -> "https://www.epey.com/laptop/..."
        if (trimmed.StartsWith("//"))
        {
            trimmed = "https:" + trimmed;
        }

        // Base URL ile göreceli URL'i birleştirerek tam mutlak URL üret
        Uri absoluteUri;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var directUri))
        {
            absoluteUri = directUri;
        }
        else
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseParsedUri))
                return null;

            if (!Uri.TryCreate(baseParsedUri, trimmed, out var combinedUri))
                return null;

            absoluteUri = combinedUri;
        }

        // Sadece HTTP ve HTTPS şemalarına izin ver
        if (absoluteUri.Scheme != Uri.UriSchemeHttp && absoluteUri.Scheme != Uri.UriSchemeHttps)
            return null;

        // Geçersiz ana makine adı kontrolü
        if (string.IsNullOrWhiteSpace(absoluteUri.Host) || !absoluteUri.Host.Contains('.'))
            return null;

        // ─────────────────────────────────────────────────────────────
        // Query String Sanitization (Temizlik)
        // Takip, reklam, oturum parametrelerini (utm_*, gclid, ref, sid vb.) kaldır
        // ─────────────────────────────────────────────────────────────
        var uriBuilder = new UriBuilder(absoluteUri);

        // Her zaman güvenli HTTPS standardına yükselt
        uriBuilder.Scheme = "https";
        if (uriBuilder.Port == 80 || uriBuilder.Port == 443)
        {
            uriBuilder.Port = -1; // Standart portu URL metninden kaldır
        }

        if (!string.IsNullOrEmpty(uriBuilder.Query))
        {
            var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);
            var cleanParams = HttpUtility.ParseQueryString(string.Empty);

            foreach (string? key in queryParams.AllKeys)
            {
                if (string.IsNullOrEmpty(key)) continue;

                // Takip/oturum parametresi değilse muhafaza et
                if (!TrackingQueryParams.Contains(key))
                {
                    cleanParams[key] = queryParams[key];
                }
            }

            uriBuilder.Query = cleanParams.Count > 0 ? cleanParams.ToString() : string.Empty;
        }

        // Fragment/Hash (#yorumlar, #ozellikler vb.) temizle
        uriBuilder.Fragment = string.Empty;

        return uriBuilder.Uri.AbsoluteUri;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 2. KALICI DETAY SAYFASI DOĞRULAMASI
    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// URL'in geçici bir arama/filtreleme sonucu veya yönlendirme linki değil,
    /// kalıcı bir ürün detay sayfası olup olmadığını doğrular.
    /// </summary>
    public static bool IsValidPermanentProductUrl(string url, TargetSiteType siteType)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        string path = uri.AbsolutePath.ToLowerInvariant();

        // 1. Ortak geçici / sahte sayfa filtreleri
        if (path == "/" || path == "/index.html" || path == "/index.php")
            return false;

        // Arama ve filtre sayfaları kalıcı ürün sayfası değildir
        if (path.Contains("/ara/") || path.Contains("/search") || path.Contains("/filter"))
            return false;

        switch (siteType)
        {
            case TargetSiteType.Akakce:
                // Akakçe geçici yönlendirme veya sponsorlu arama linkleri:
                // Örnek sahte/geçici: "/c/?s=...", "/kampanyalar/", "/kategori/"
                if (path.StartsWith("/c/") || path.StartsWith("/c/?") || path.StartsWith("/kampanya"))
                    return false;

                // Akakçe kalıcı ürün detay sayfaları ".html" ile biter ve kategori/ürün slug'ı içerir.
                // Örnek: "https://www.akakce.com/laptop/en-ucuz-lenovo-loq-15iax9-fiyati,654321.html"
                if (!path.EndsWith(".html"))
                    return false;

                // Sadece "/laptop/" veya "/oyun-bilgisayari/" ana kategori liste sayfası olmamalı
                if (path == "/laptop/" || path == "/oyun-bilgisayari/" || path == "/laptop.html")
                    return false;

                return true;

            case TargetSiteType.Epey:
                // Epey geçici dışa yönlendirme linkleri:
                // Örnek sahte: "/git/...", "/yonlendir/..."
                if (path.StartsWith("/git/") || path.StartsWith("/yonlendir/"))
                    return false;

                // Epey kalıcı detay sayfaları ".html" ile biter
                // Örnek: "https://www.epey.com/laptop/lenovo-loq-15iax9.html"
                if (!path.EndsWith(".html"))
                    return false;

                // Ana kategori listesi olmamalı
                if (path == "/laptop/" || path == "/laptop/oyuncu-laptop/")
                    return false;

                return true;

            default:
                return path.EndsWith(".html") || path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length >= 2;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // YARDIMCI METOTLAR (FİYAT, MARKA, VERİTABANI UPSERT)
    // ─────────────────────────────────────────────────────────────────────────────
    private static decimal ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        string cleaned = Regex.Replace(raw, @"[^\d,\.]", "");
        cleaned = cleaned.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal result)
            ? result : 0;
    }

    private static string ExtractBrand(string fullName)
    {
        string[] knownBrands = ["Apple", "ASUS", "Lenovo", "Dell", "HP", "MSI",
                                 "Acer", "Razer", "Microsoft", "Samsung", "LG", "Huawei", "Monster", "Casper"];
        foreach (var brand in knownBrands)
            if (fullName.StartsWith(brand, StringComparison.OrdinalIgnoreCase))
                return brand;

        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private static string CleanModelName(string raw)
    {
        return Regex.Replace(raw, @"\s+", " ").Trim();
    }

    private async Task UpsertLaptopAsync(AppDbContext db, ScrapedLaptop item, CancellationToken ct)
    {
        // SourceUrl veya (Brand + Model) eşleşmesine göre ara
        var existing = await db.Laptops
            .FirstOrDefaultAsync(l => l.SourceUrl == item.SourceUrl ||
                                     (l.Brand == item.Brand && l.Model == item.Model), ct);

        if (existing is not null)
        {
            existing.Price = item.Price;
            existing.SourceUrl = item.SourceUrl; // Güncel kalıcı URL ile yenile
            existing.LastUpdatedAt = DateTime.UtcNow;
            existing.IsActive = true;
        }
        else
        {
            db.Laptops.Add(new Laptop
            {
                Brand = item.Brand,
                Model = item.Model,
                Price = item.Price,
                CpuScore = 0,
                GpuScore = 0,
                RamCapacityGB = 0,
                WeightKG = 0,
                BatteryLifeHours = 0,
                SourceUrl = item.SourceUrl, // Kalıcı ve doğrulanmış URL
                IsActive = false, // Yeni scraped ürünler benchmark tamamlanana kadar pasif
                LastUpdatedAt = DateTime.UtcNow
            });
        }
    }

    public enum TargetSiteType
    {
        Akakce,
        Epey,
        Other
    }

    public record ScrapeTarget(string Url, TargetSiteType SiteType);

    public record ScrapedLaptop
    {
        public string Brand { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string SourceUrl { get; init; } = string.Empty;
    }
}
