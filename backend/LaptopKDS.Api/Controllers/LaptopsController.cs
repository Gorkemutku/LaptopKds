using LaptopKDS.Api.Data;
using LaptopKDS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopKDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LaptopsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LaptopsController(AppDbContext db) => _db = db;

    /// <summary>Tüm aktif laptopları listeler</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = _db.Laptops.AsQueryable();
        if (activeOnly) query = query.Where(l => l.IsActive);
        return Ok(await query.OrderBy(l => l.Brand).ThenBy(l => l.Model).ToListAsync());
    }

    /// <summary>ID'ye göre laptop getirir</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var laptop = await _db.Laptops.FindAsync(id);
        return laptop is null ? NotFound() : Ok(laptop);
    }

    /// <summary>Yeni laptop ekler</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Laptop laptop)
    {
        laptop.LastUpdatedAt = DateTime.UtcNow;
        _db.Laptops.Add(laptop);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = laptop.Id }, laptop);
    }

    /// <summary>Laptop günceller</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Laptop laptop)
    {
        if (id != laptop.Id) return BadRequest();
        laptop.LastUpdatedAt = DateTime.UtcNow;
        _db.Entry(laptop).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Laptop'u pasife alır (soft delete)</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var laptop = await _db.Laptops.FindAsync(id);
        if (laptop is null) return NotFound();
        laptop.IsActive = false;
        laptop.LastUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
