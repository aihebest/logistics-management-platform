using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/fuel")]
[Authorize]
public class FuelController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FuelLogDto>> GetAll(
        [FromQuery] Guid? vehicleId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? productType,
        [FromQuery] Guid? locationId)
    {
        var q = db.FuelLogs
            .Include(f => f.Vehicle)
            .Include(f => f.LoggedBy)
            .Include(f => f.Location)
            .AsQueryable();

        if (vehicleId.HasValue)   q = q.Where(f => f.VehicleId == vehicleId);
        if (from.HasValue)        q = q.Where(f => f.FuelDate >= from.Value);
        if (to.HasValue)          q = q.Where(f => f.FuelDate <= to.Value);
        if (!string.IsNullOrEmpty(productType)) q = q.Where(f => f.ProductType == productType);
        if (locationId.HasValue)  q = q.Where(f => f.LocationId == locationId);

        return await q.OrderByDescending(f => f.FuelDate).Select(f => ToDto(f)).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<FuelLogDto>> Create(CreateFuelLogDto dto)
    {
        var vehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized();

        var totalCost = dto.LitresFilled * dto.CostPerLitre;

        int? mileageCovered = null;
        if (dto.OdometerTo.HasValue && dto.OdometerFrom.HasValue && dto.OdometerTo > dto.OdometerFrom)
            mileageCovered = dto.OdometerTo.Value - dto.OdometerFrom.Value;

        var log = new Models.Entities.FuelLog
        {
            Id = Guid.NewGuid(),
            VehicleId = dto.VehicleId,
            LoggedById = caller.Id,
            FuelDate = dto.FuelDate,
            ProductType = dto.ProductType,
            LitresFilled = dto.LitresFilled,
            CostPerLitre = dto.CostPerLitre,
            TotalCost = totalCost,
            IsCashPayment = dto.IsCashPayment,
            OdometerAtFill = dto.OdometerAtFill,
            OdometerFrom = dto.OdometerFrom,
            OdometerTo = dto.OdometerTo,
            MileageCovered = mileageCovered,
            FuelGaugeBefore = dto.FuelGaugeBefore,
            FuelGaugeAfter = dto.FuelGaugeAfter,
            CostCentre = dto.CostCentre,
            StationName = dto.StationName,
            Notes = dto.Notes,
            LocationId = dto.LocationId,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.OdometerAtFill > vehicle.OdometerKm)
        {
            vehicle.OdometerKm = dto.OdometerAtFill;
            vehicle.UpdatedAt = DateTime.UtcNow;
        }

        db.FuelLogs.Add(log);
        await db.SaveChangesAsync();

        // Reload location name for response
        if (log.LocationId.HasValue)
            log.Location = await db.Locations.FindAsync(log.LocationId);

        log.Vehicle = vehicle;
        log.LoggedBy = caller;

        return CreatedAtAction(nameof(GetAll), new { id = log.Id }, ToDto(log));
    }

    private static FuelLogDto ToDto(Models.Entities.FuelLog f) => new(
        f.Id, f.VehicleId, f.Vehicle.RegistrationNo,
        f.LoggedBy?.FullName ?? "",
        f.FuelDate,
        f.ProductType ?? "PMS",
        f.LitresFilled, f.CostPerLitre, f.TotalCost,
        f.IsCashPayment,
        f.OdometerAtFill,
        f.OdometerFrom, f.OdometerTo, f.MileageCovered,
        f.FuelGaugeBefore, f.FuelGaugeAfter,
        f.CostCentre, f.StationName, f.ReceiptBlobUrl, f.Notes,
        f.LocationId, f.Location?.Name,
        f.CreatedAt);
}
