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
public class FuelController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit,
    ILogger<FuelController> logger) : ControllerBase
{
    /// <summary>
    /// Corrects an existing fuel log.
    ///
    /// Restricted to operations staff, and every change is written to the audit
    /// trail with the old and new values. These figures reconcile against vendor
    /// invoices and go to accounts, so a silent edit would be indefensible at
    /// audit — the record must show who changed what, when and why.
    ///
    /// Total cost and mileage covered are recalculated rather than accepted from
    /// the client, so they can never disagree with the values they derive from.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateFuelLogDto dto)
    {
        var log = await db.FuelLogs.Include(f => f.Vehicle).FirstOrDefaultAsync(f => f.Id == id);
        if (log == null) return NotFound();

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var changes = new List<string>();
        void Track<T>(string field, T oldValue, T newValue)
        {
            if (!Equals(oldValue, newValue))
                changes.Add($"{field}: {oldValue?.ToString() ?? "—"} → {newValue?.ToString() ?? "—"}");
        }

        if (dto.VehicleId.HasValue && dto.VehicleId.Value != log.VehicleId)
        {
            var vehicle = await db.Vehicles.FindAsync(dto.VehicleId.Value);
            if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });
            Track("Vehicle", log.Vehicle?.RegistrationNo, vehicle.RegistrationNo);
            log.VehicleId = dto.VehicleId.Value;
        }

        if (dto.FuelDate.HasValue)        { Track("Date", log.FuelDate, dto.FuelDate.Value);               log.FuelDate = dto.FuelDate.Value; }
        if (dto.ProductType != null)      { Track("Product", log.ProductType, dto.ProductType);            log.ProductType = dto.ProductType; }
        if (dto.LitresFilled.HasValue)    { Track("Litres", log.LitresFilled, dto.LitresFilled.Value);     log.LitresFilled = dto.LitresFilled.Value; }
        if (dto.CostPerLitre.HasValue)    { Track("Rate", log.CostPerLitre, dto.CostPerLitre.Value);       log.CostPerLitre = dto.CostPerLitre.Value; }
        if (dto.OdometerAtFill.HasValue)  { Track("Odometer", log.OdometerAtFill, dto.OdometerAtFill.Value); log.OdometerAtFill = dto.OdometerAtFill.Value; }
        if (dto.OdometerFrom.HasValue)    { Track("Odometer From", log.OdometerFrom, dto.OdometerFrom);    log.OdometerFrom = dto.OdometerFrom; }
        if (dto.OdometerTo.HasValue)      { Track("Odometer To", log.OdometerTo, dto.OdometerTo);          log.OdometerTo = dto.OdometerTo; }
        if (dto.FuelGaugeBefore.HasValue) { Track("Gauge Before", log.FuelGaugeBefore, dto.FuelGaugeBefore); log.FuelGaugeBefore = dto.FuelGaugeBefore; }
        if (dto.FuelGaugeAfter.HasValue)  { Track("Gauge After", log.FuelGaugeAfter, dto.FuelGaugeAfter);  log.FuelGaugeAfter = dto.FuelGaugeAfter; }
        if (dto.IsCashPayment.HasValue)   { Track("Payment", log.IsCashPayment ? "Cash" : "Card/Transfer", dto.IsCashPayment.Value ? "Cash" : "Card/Transfer"); log.IsCashPayment = dto.IsCashPayment.Value; }
        if (dto.CostCentre != null)       { Track("Cost Centre", log.CostCentre, dto.CostCentre);          log.CostCentre = dto.CostCentre; }
        if (dto.StationName != null)      { Track("Station", log.StationName, dto.StationName);            log.StationName = dto.StationName; }
        if (dto.Notes != null)            { Track("Notes", log.Notes, dto.Notes);                          log.Notes = dto.Notes; }
        if (dto.LocationId.HasValue)      { Track("Location", log.LocationId, dto.LocationId);             log.LocationId = dto.LocationId; }

        if (changes.Count == 0)
            return Ok(new { message = "No changes were made." });

        // Derived values — always recalculated so they cannot drift.
        var recalculatedTotal = log.LitresFilled * log.CostPerLitre;
        if (recalculatedTotal != log.TotalCost)
        {
            changes.Add($"Total: {log.TotalCost} → {recalculatedTotal}");
            log.TotalCost = recalculatedTotal;
        }

        if (log.OdometerFrom.HasValue && log.OdometerTo.HasValue && log.OdometerTo >= log.OdometerFrom)
            log.MileageCovered = log.OdometerTo - log.OdometerFrom;

        await db.SaveChangesAsync();

        var reason = string.IsNullOrWhiteSpace(dto.CorrectionReason) ? "No reason given" : dto.CorrectionReason.Trim();
        await audit.LogAsync("FuelLog", id.ToString(), "Corrected",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"Reason: {reason}. Changes — {string.Join("; ", changes)}");

        logger.LogInformation("Fuel log {Id} corrected by {Email}: {Changes}",
            id, caller.Email, string.Join("; ", changes));

        return Ok(new { message = "Fuel log updated.", changes });
    }

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
