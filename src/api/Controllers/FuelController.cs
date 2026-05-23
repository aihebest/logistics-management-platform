using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/fuel")]
[Authorize]
public class FuelController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FuelLogDto>> GetAll(
        [FromQuery] Guid? vehicleId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var q = db.FuelLogs.Include(f => f.Vehicle).Include(f => f.LoggedBy).AsQueryable();
        if (vehicleId.HasValue) q = q.Where(f => f.VehicleId == vehicleId);
        if (from.HasValue) q = q.Where(f => f.FuelDate >= from.Value);
        if (to.HasValue) q = q.Where(f => f.FuelDate <= to.Value);

        return await q.OrderByDescending(f => f.FuelDate).Select(f => ToDto(f)).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<FuelLogDto>> Create(CreateFuelLogDto dto)
    {
        var vehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });

        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized();

        var totalCost = dto.LitresFilled * dto.CostPerLitre;
        var log = new Models.Entities.FuelLog
        {
            Id = Guid.NewGuid(),
            VehicleId = dto.VehicleId,
            LoggedById = caller.Id,
            FuelDate = dto.FuelDate,
            LitresFilled = dto.LitresFilled,
            CostPerLitre = dto.CostPerLitre,
            TotalCost = totalCost,
            OdometerAtFill = dto.OdometerAtFill,
            StationName = dto.StationName,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // Update vehicle odometer
        if (dto.OdometerAtFill > vehicle.OdometerKm)
        {
            vehicle.OdometerKm = dto.OdometerAtFill;
            vehicle.UpdatedAt = DateTime.UtcNow;
        }

        db.FuelLogs.Add(log);
        await db.SaveChangesAsync();
        log.Vehicle = vehicle;
        log.LoggedBy = caller;

        return CreatedAtAction(nameof(GetAll), new { id = log.Id }, ToDto(log));
    }

    private static FuelLogDto ToDto(Models.Entities.FuelLog f) => new(
        f.Id, f.VehicleId, f.Vehicle.RegistrationNo,
        f.LoggedBy?.FullName ?? "",
        f.FuelDate, f.LitresFilled, f.CostPerLitre, f.TotalCost,
        f.OdometerAtFill, f.StationName, f.ReceiptBlobUrl, f.Notes, f.CreatedAt);
}
