using LaptopKDS.Api.DTOs;
using LaptopKDS.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaptopKDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RecommendationController : ControllerBase
{
    private readonly TopsisService _topsisService;
    private readonly ILogger<RecommendationController> _logger;

    public RecommendationController(TopsisService topsisService, ILogger<RecommendationController> logger)
    {
        _topsisService = topsisService;
        _logger = logger;
    }

    /// <summary>
    /// Bütçe ve profile göre TOPSIS algoritmasıyla sıralanmış laptop önerilerini döner.
    /// </summary>
    /// <param name="request">BudgetMax: Maksimum bütçe (TL), ProfileId: Karar profili ID'si</param>
    [HttpPost]
    [ProducesResponseType(typeof(List<LaptopRecommendationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecommendations([FromBody] RecommendationRequestDto request)
    {
        if (request.BudgetMax <= 0)
            return BadRequest(new { error = "Bütçe sıfırdan büyük olmalıdır." });

        if (request.ProfileId <= 0)
            return BadRequest(new { error = "Geçerli bir profil seçiniz." });

        try
        {
            var results = await _topsisService.GetRecommendationsAsync(request);
            return Ok(new
            {
                totalFound = results.Count,
                budgetMax = request.BudgetMax,
                profileId = request.ProfileId,
                recommendations = results
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Profil bulunamadı: {ProfileId}", request.ProfileId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOPSIS hesaplama hatası");
            return StatusCode(500, new { error = "Hesaplama sırasında bir hata oluştu." });
        }
    }
}
