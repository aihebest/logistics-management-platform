using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/movement-register")]
[Authorize]
public class MovementRegisterController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<MovementRegisterDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? movementType,
        [FromQuery] DateOnly? date)
    {
        var q = db.MovementRegisters
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.LoggedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);
        if (!string.IsNullOrEmpty(movementType)) q = q.Where(r => r.MovementType == movementType);
        if (date.HasValue)
        {
            var dayStart = date.Value.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.Value.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(r => r.MovementDateTime >= dayStart && r.MovementDateTime <= dayEnd);
        }

        return await q.OrderByDescending(r => r.MovementDateTime).Select(r => ToDto(r)).ToListAsync();
    }

    /// <summary>
    /// Vehicle-grouped movement summary for vendor and accounts reconciliation.
    /// One block per vehicle with its movements and a distance total, so the
    /// sheet can be printed and issued with fuel vouchers instead of being
    /// maintained by hand in Excel.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<MovementRegisterSummaryDto>> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? vehicleReg)
    {
        // Default to the current calendar month — the usual reconciliation period.
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? new DateOnly(today.Year, today.Month, 1);
        var toDate   = to   ?? today;

        if (toDate < fromDate)
            return BadRequest(new { error = "The 'to' date cannot be earlier than the 'from' date." });

        var start = fromDate.ToDateTime(TimeOnly.MinValue);
        var end   = toDate.ToDateTime(TimeOnly.MaxValue);

        var q = db.MovementRegisters
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Where(r => r.MovementDateTime >= start && r.MovementDateTime <= end
                     && r.VehicleId != null);

        if (!string.IsNullOrWhiteSpace(vehicleReg))
            q = q.Where(r => r.Vehicle!.RegistrationNo == vehicleReg);

        var records = await q.OrderBy(r => r.MovementDateTime).ToListAsync();

        var blocks = records
            .GroupBy(r => r.Vehicle!.RegistrationNo)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var lines = g.Select(r =>
                {
                    int? dist = (r.MileageIn.HasValue && r.MileageOut.HasValue
                                 && r.MileageIn.Value >= r.MileageOut.Value)
                        ? r.MileageIn.Value - r.MileageOut.Value
                        : null;

                    return new MovementSummaryLineDto(
                        r.MovementDateTime, r.ReturnDateTime, r.Purpose, r.Passengers,
                        r.Origin ?? "", r.Destination ?? "",
                        r.Driver?.FullName, r.RelatedRefNo, r.GatePassNo,
                        r.MileageOut, r.MileageIn, dist, r.Status);
                }).ToList();

                var outs = g.Where(x => x.MileageOut.HasValue).Select(x => x.MileageOut!.Value).ToList();
                var ins  = g.Where(x => x.MileageIn.HasValue).Select(x => x.MileageIn!.Value).ToList();

                return new VehicleMovementSummaryDto(
                    VehicleReg:        g.Key,
                    TripCount:         lines.Count,
                    TotalDistanceKm:   lines.Sum(l => l.DistanceKm ?? 0),
                    OpeningOdometer:   outs.Count > 0 ? outs.Min() : null,
                    ClosingOdometer:   ins.Count  > 0 ? ins.Max()  : null,
                    OpenMovements:     lines.Count(l => l.Status != "Closed"),
                    Movements:         lines);
            })
            .ToList();

        return new MovementRegisterSummaryDto(
            FromDate:             fromDate,
            ToDate:               toDate,
            VehicleCount:         blocks.Count,
            TotalTrips:           blocks.Sum(b => b.TripCount),
            GrandTotalDistanceKm: blocks.Sum(b => b.TotalDistanceKm),
            Vehicles:             blocks);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovementRegisterDto>> Get(Guid id)
    {
        var r = await db.MovementRegisters
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.LoggedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
        return r == null ? NotFound() : ToDto(r);
    }

    [HttpPost]
    public async Task<ActionResult<MovementRegisterDto>> Create(CreateMovementRegisterDto dto)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized();

        var entry = new MovementRegister
        {
            Id = Guid.NewGuid(),
            MovementType = dto.MovementType,
            // Only meaningful when the type is "Other" — ignore stale text otherwise.
            MovementTypeOther = dto.MovementType == "Other"
                ? (string.IsNullOrWhiteSpace(dto.MovementTypeOther) ? null : dto.MovementTypeOther.Trim())
                : null,
            Passengers = string.IsNullOrWhiteSpace(dto.Passengers) ? null : dto.Passengers.Trim(),
            VehicleId = dto.VehicleId,
            DriverId = dto.DriverId,
            RelatedRefNo = dto.RelatedRefNo,
            Purpose = dto.Purpose,
            Origin = dto.Origin,
            Destination = dto.Destination,
            MovementDateTime = dto.MovementDateTime,
            MileageOut = dto.MileageOut,
            MileageIn = dto.MileageIn,
            ReturnDateTime = dto.ReturnDateTime,
            GatePassNo = dto.GatePassNo,
            Status = dto.ReturnDateTime.HasValue ? "Closed" : "Open",
            LoggedById = caller.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.MovementRegisters.Add(entry);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = entry.Id },
            await GetFullDto(entry.Id));
    }

    [HttpPatch("{id:guid}/close")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Close(Guid id, CloseMovementDto dto)
    {
        var entry = await db.MovementRegisters.FindAsync(id);
        if (entry == null) return NotFound();

        entry.ReturnDateTime = dto.ReturnDateTime;
        if (dto.MileageIn.HasValue) entry.MileageIn = dto.MileageIn;
        entry.Status = "Closed";
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<MovementRegisterDto> GetFullDto(Guid id)
    {
        var r = await db.MovementRegisters
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.LoggedBy)
            .FirstAsync(x => x.Id == id);
        return ToDto(r);
    }

    private static MovementRegisterDto ToDto(MovementRegister r) => new(
        r.Id,
        r.MovementType,
        r.MovementTypeOther,
        r.Passengers,
        r.Vehicle?.RegistrationNo,
        r.Driver?.FullName,
        r.RelatedRefNo,
        r.Purpose,
        r.Origin ?? string.Empty,
        r.Destination ?? string.Empty,
        r.MovementDateTime,
        r.ReturnDateTime,
        r.MileageOut,
        r.MileageIn,
        // Distance covered — only meaningful once the vehicle is back in and
        // the closing odometer is at or above the opening reading.
        (r.MileageIn.HasValue && r.MileageOut.HasValue && r.MileageIn.Value >= r.MileageOut.Value)
            ? r.MileageIn.Value - r.MileageOut.Value
            : null,
        r.GatePassNo,
        r.Status,
        r.LoggedBy?.FullName ?? "",
        r.CreatedAt);
}
