using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/movement-register")]
[Authorize]
public class MovementRegisterController(AppDbContext db) : ControllerBase
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
        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized();

        var entry = new MovementRegister
        {
            Id = Guid.NewGuid(),
            MovementType = dto.MovementType,
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
        r.GatePassNo,
        r.Status,
        r.LoggedBy?.FullName ?? "",
        r.CreatedAt);
}
