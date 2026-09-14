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
        if (dto.OdometerAtFill.HasValue)    { Track("Mileage Before", log.OdometerAtFill, dto.OdometerAtFill.Value); log.OdometerAtFill = dto.OdometerAtFill.Value; }
        if (dto.OdometerAfterFill.HasValue) { Track("Mileage After", log.OdometerAfterFill, dto.OdometerAfterFill); log.OdometerAfterFill = dto.OdometerAfterFill; }

        // Sending an empty string clears the reading; sending something we don't
        // recognise is a mistake worth reporting.
        if (dto.FuelGaugeBeforePosition != null)
        {
            if (!TryReadGauge(dto.FuelGaugeBeforePosition, out var pos)) return BadRequest(GaugePositionError());
            Track("Gauge Before", log.FuelGaugeBeforePosition, pos);
            log.FuelGaugeBeforePosition = pos;
        }

        if (dto.FuelGaugeAfterPosition != null)
        {
            if (!TryReadGauge(dto.FuelGaugeAfterPosition, out var pos)) return BadRequest(GaugePositionError());
            Track("Gauge After", log.FuelGaugeAfterPosition, pos);
            log.FuelGaugeAfterPosition = pos;
        }
        if (dto.PaymentMethod != null)
        {
            var method = NormalisePaymentMethod(dto.PaymentMethod);
            if (method == null)
                return BadRequest(new { error = $"Payment method must be one of: {string.Join(", ", PaymentMethods)}." });
            Track("Payment", log.PaymentMethod, method);
            log.PaymentMethod = method;
            log.IsCashPayment = method == "Cash";   // keep the legacy flag consistent
        }
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

        // KM covered is always derived, never accepted from the client. A lower
        // "after" reading means one of the two was mistyped, so leave the
        // distance blank rather than publishing a negative number.
        log.MileageCovered = log.OdometerAfterFill.HasValue && log.OdometerAfterFill >= log.OdometerAtFill
            ? log.OdometerAfterFill - log.OdometerAtFill
            : null;

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

        var paymentMethod = NormalisePaymentMethod(dto.PaymentMethod);
        if (paymentMethod == null)
            return BadRequest(new { error = $"Payment method must be one of: {string.Join(", ", PaymentMethods)}." });

        // Blank means "not recorded" and is allowed; only an unrecognised reading
        // is an error.
        var gaugeBefore = NormaliseGaugePosition(dto.FuelGaugeBeforePosition);
        if (!string.IsNullOrWhiteSpace(dto.FuelGaugeBeforePosition) && gaugeBefore == null)
            return BadRequest(GaugePositionError());

        var gaugeAfter = NormaliseGaugePosition(dto.FuelGaugeAfterPosition);
        if (!string.IsNullOrWhiteSpace(dto.FuelGaugeAfterPosition) && gaugeAfter == null)
            return BadRequest(GaugePositionError());

        var totalCost = dto.LitresFilled * dto.CostPerLitre;

        // KM covered is derived here, not sent by the client, so the column always
        // agrees with the two readings beside it.
        int? mileageCovered = dto.OdometerAfterFill.HasValue && dto.OdometerAfterFill >= dto.OdometerAtFill
            ? dto.OdometerAfterFill.Value - dto.OdometerAtFill
            : null;

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
            PaymentMethod = paymentMethod,
            IsCashPayment = paymentMethod == "Cash",   // legacy flag kept in step
            OdometerAtFill = dto.OdometerAtFill,
            OdometerAfterFill = dto.OdometerAfterFill,
            MileageCovered = mileageCovered,
            FuelGaugeBeforePosition = gaugeBefore,
            FuelGaugeAfterPosition = gaugeAfter,
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

    /// <summary>
    /// The four ways fuel gets paid for, as confirmed by the Director of Logistics.
    /// </summary>
    private static readonly string[] PaymentMethods = ["Card", "Cash", "Credit", "Transfer"];

    /// <summary>
    /// Tank positions, lowest to highest, in the wording the logistics team
    /// already uses on their own fuel report. Order matters — the UI renders the
    /// dropdown straight from this list, so it reads like a fuel gauge.
    /// </summary>
    private static readonly string[] GaugePositions =
    [
        "Reserve",
        "Below 1/4 tank",
        "1/4 tank",
        "Below 1/2 tank",
        "1/2 tank",
        "Above 1/2 tank",
        "3/4 tank",
        "Above 3/4 tank",
        "Full tank",
    ];

    /// <summary>
    /// Accepts any casing and tolerates the spacing variations that appear in the
    /// team's spreadsheet ("1/4  tank", "Below 1/4"). Returns the canonical value,
    /// or null when the reading isn't one we recognise. Blank means "not recorded".
    /// </summary>
    private static string? NormaliseGaugePosition(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        static string Key(string s) =>
            string.Concat(s.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();

        var key = Key(value);

        var exact = GaugePositions.FirstOrDefault(p => Key(p) == key);
        if (exact != null) return exact;

        // The spreadsheet writes "Below 1/4" without the trailing word.
        return GaugePositions.FirstOrDefault(p => Key(p) == key + "tank");
    }

    /// <summary>
    /// Reads a gauge value supplied on a correction. Blank clears the reading and
    /// succeeds with null; an unrecognised value fails.
    /// </summary>
    private static bool TryReadGauge(string value, out string? position)
    {
        if (string.IsNullOrWhiteSpace(value)) { position = null; return true; }
        position = NormaliseGaugePosition(value);
        return position != null;
    }

    private static object GaugePositionError() => new
    {
        error = $"Fuel gauge reading must be one of: {string.Join(", ", GaugePositions)}."
    };

    /// <summary>
    /// Accepts any casing and returns the canonical value, or null when the value
    /// isn't one we recognise. Blank falls back to Card, which is the common case.
    /// </summary>
    private static string? NormalisePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Card";
        return PaymentMethods.FirstOrDefault(m => string.Equals(m, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static FuelLogDto ToDto(Models.Entities.FuelLog f) => new(
        f.Id, f.VehicleId, f.Vehicle.RegistrationNo,
        f.LoggedBy?.FullName ?? "",
        f.FuelDate,
        f.ProductType ?? "PMS",
        f.LitresFilled, f.CostPerLitre, f.TotalCost,
        string.IsNullOrWhiteSpace(f.PaymentMethod) ? (f.IsCashPayment ? "Cash" : "Card") : f.PaymentMethod,
        f.IsCashPayment,
        f.OdometerAtFill,
        f.OdometerAfterFill,
        f.MileageCovered,
        f.FuelGaugeBeforePosition,
        f.FuelGaugeAfterPosition,
        f.CostCentre, f.StationName, f.ReceiptBlobUrl, f.Notes,
        f.LocationId, f.Location?.Name,
        f.CreatedAt);
}
