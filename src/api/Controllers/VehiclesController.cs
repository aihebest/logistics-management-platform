using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<VehicleDto>> GetAll([FromQuery] string? status)
    {
        var query = db.Vehicles
            .Include(v => v.AssignedMechanic)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(v => v.Status == status);

        return await query
            .OrderBy(v => v.RegistrationNo)
            .Select(v => ToDto(v))
            .ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> Get(Guid id)
    {
        var v = await db.Vehicles.Include(x => x.AssignedMechanic).FirstOrDefaultAsync(x => x.Id == id);
        return v == null ? NotFound() : ToDto(v);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<VehicleDto>> Create(CreateVehicleDto dto)
    {
        if (await db.Vehicles.AnyAsync(v => v.RegistrationNo == dto.RegistrationNo))
            return Conflict(new { error = "Registration number already exists" });

        var vehicle = new Models.Entities.Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationNo = dto.RegistrationNo,
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            FuelType = dto.FuelType,
            OdometerKm = dto.OdometerKm,
            MileageAtPurchase = dto.MileageAtPurchase,
            PreviousMileageAtPurchase = dto.PreviousMileageAtPurchase,
            ServiceIntervalKm = dto.ServiceIntervalKm,
            ChassisNo = dto.ChassisNo,
            PurchaseYear = dto.PurchaseYear,
            Colour = dto.Colour,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = vehicle.Id }, ToDto(vehicle));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Manager,Admin,Mechanic")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleDto dto)
    {
        var vehicle = await db.Vehicles.FindAsync(id);
        if (vehicle == null) return NotFound();

        if (dto.Status != null) vehicle.Status = dto.Status;
        if (dto.OdometerKm.HasValue) vehicle.OdometerKm = dto.OdometerKm.Value;
        if (dto.LastServiceDate.HasValue) vehicle.LastServiceDate = dto.LastServiceDate;
        if (dto.NextServiceDate.HasValue) vehicle.NextServiceDate = dto.NextServiceDate;
        if (dto.AssignedMechanicId.HasValue) vehicle.AssignedMechanicId = dto.AssignedMechanicId;
        if (dto.ChassisNo != null) vehicle.ChassisNo = dto.ChassisNo;
        if (dto.PurchaseYear.HasValue) vehicle.PurchaseYear = dto.PurchaseYear;
        if (dto.Colour != null) vehicle.Colour = dto.Colour;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static VehicleDto ToDto(Models.Entities.Vehicle v) => new(
        v.Id, v.RegistrationNo, v.Make, v.Model, v.Year, v.Status, v.FuelType,
        v.OdometerKm, v.MileageAtPurchase, v.PreviousMileageAtPurchase,
        v.ServiceIntervalKm, v.LastServiceDate, v.NextServiceDate,
        v.AssignedMechanic?.FullName,
        v.ChassisNo, v.PurchaseYear, v.Colour,
        DateTime.UtcNow.Year - v.Year);
}
