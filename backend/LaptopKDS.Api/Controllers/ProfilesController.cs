using LaptopKDS.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopKDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProfilesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfilesController(AppDbContext db) => _db = db;

    /// <summary>Tüm karar profillerini (Yazılımcı, Oyuncu, Öğrenci, Tasarımcı) döner</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var profiles = await _db.DecisionProfiles
            .Include(p => p.CriterionWeights)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.IconEmoji,
                CriterionWeights = p.CriterionWeights.Select(w => new
                {
                    w.WeightPrice,
                    w.WeightCpuScore,
                    w.WeightGpuScore,
                    w.WeightRamCapacity,
                    w.WeightKG,
                    w.WeightBattery
                })
            })
            .ToListAsync();

        return Ok(profiles);
    }

    /// <summary>ID'ye göre profil getirir</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var profile = await _db.DecisionProfiles
            .Include(p => p.CriterionWeights)
            .FirstOrDefaultAsync(p => p.Id == id);
        return profile is null ? NotFound() : Ok(profile);
    }
}
