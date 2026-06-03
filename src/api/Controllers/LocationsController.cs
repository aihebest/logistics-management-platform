using LogisticsApi.Data;
using LogisticsApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

public record LocationDto(Guid Id, string Name, string Code, bool IsActive);
public record CreateLocationDto(string Name, string Code);

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<LocationDto>> GetAll([FromQuery] bool activeOnly = true)
    {
        var q = db.Locations.AsQueryable();
        if (activeOnly) q = q.Where(l => l.IsActive);
        return await q.OrderBy(l => l.Name)
            .Select(l => new LocationDto(l.Id, l.Name, l.Code, l.IsActive))
            .ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LocationDto>> Create(CreateLocationDto dto)
    {
        if (await db.Locations.AnyAsync(l => l.Code == dto.Code.ToUpperInvariant()))
            return Conflict(new { error = "A location with this code already exists." });

        var loc = new Location
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Code = dto.Code.ToUpperInvariant().Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Locations.Add(loc);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = loc.Id },
            new LocationDto(loc.Id, loc.Name, loc.Code, loc.IsActive));
    }

    [HttpPatch("{id:guid}/toggle")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var loc = await db.Locations.FindAsync(id);
        if (loc == null) return NotFound();
        loc.IsActive = !loc.IsActive;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
