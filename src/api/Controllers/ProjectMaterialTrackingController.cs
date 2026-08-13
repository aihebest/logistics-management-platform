using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/project-materials")]
[Authorize]
public class ProjectMaterialTrackingController(
    AppDbContext db,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ProjectMaterialTrackingDto>> GetAll(
        [FromQuery] int? year,
        [FromQuery] string? project,
        [FromQuery] string? status)
    {
        var q = db.ProjectMaterialTrackings.AsQueryable();

        if (year.HasValue) q = q.Where(m => m.TrackingYear == year.Value);
        else q = q.Where(m => m.TrackingYear == DateTime.UtcNow.Year);

        if (!string.IsNullOrEmpty(project)) q = q.Where(m => m.Project == project);
        if (!string.IsNullOrEmpty(status)) q = q.Where(m => m.DeliveryStatus == status);

        return await q.OrderBy(m => m.PoNumber).Select(m => ToDto(m)).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectMaterialTrackingDto>> Get(Guid id)
    {
        var m = await db.ProjectMaterialTrackings.FindAsync(id);
        return m == null ? NotFound() : ToDto(m);
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<ActionResult<ProjectMaterialTrackingDto>> Create(CreateProjectMaterialTrackingDto dto)
    {
        // CreatedById is a required foreign key. It was never populated, so every
        // insert wrote Guid.Empty and failed the constraint — which is why this
        // register could never accept an entry.
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null)
            return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var entry = new ProjectMaterialTracking
        {
            Id = Guid.NewGuid(),
            CreatedById = caller.Id,
            TrackingYear = dto.TrackingYear,
            PoNumber = dto.PoNumber,
            PoLineItem = dto.PoLineItem,
            Project = dto.Project ?? string.Empty,
            Buyer = dto.Buyer ?? string.Empty,
            Description = dto.Description,
            Quantity = dto.Quantity,
            Supplier = dto.Supplier,
            FreightForwarder = dto.FreightForwarder,
            ReadinessDate = dto.ReadinessDate,
            ModeOfTransport = dto.ModeOfTransport,
            DeliveryStatus = string.IsNullOrWhiteSpace(dto.DeliveryStatus) ? "Pending" : dto.DeliveryStatus,
            // Shipping / tracking detail — optional, for consignments already in flight
            FormMNumber = dto.FormMNumber,
            VesselName = dto.VesselName,
            Etd = dto.Etd,
            Eta = dto.Eta,
            ActualDeliveryDate = dto.ActualDeliveryDate,
            Remarks = dto.Remarks,
            // ISO audit fields
            ExpectedDeliveryDateProjectTeam = dto.ExpectedDeliveryDateProjectTeam,
            StoreNotificationDate = dto.StoreNotificationDate,
            ExpectedDeliveryDateStoreTeam = dto.ExpectedDeliveryDateStoreTeam,
            ExpectedDeliveryDateAgreed = dto.ExpectedDeliveryDateAgreed,
            PaarNumber = dto.PaarNumber,
            PaarDate = dto.PaarDate,
            BlNumber = dto.BlNumber,
            AwbNumber = dto.AwbNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.ProjectMaterialTrackings.Add(entry);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, ToDto(entry));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectMaterialTrackingDto dto)
    {
        var entry = await db.ProjectMaterialTrackings.FindAsync(id);
        if (entry == null) return NotFound();

        if (dto.DeliveryStatus != null) entry.DeliveryStatus = dto.DeliveryStatus;
        if (dto.PickupAuthDate.HasValue) entry.PickupAuthDate = dto.PickupAuthDate;
        if (dto.PickupDate.HasValue) entry.PickupDate = dto.PickupDate;
        if (dto.FormMNumber != null) entry.FormMNumber = dto.FormMNumber;
        if (dto.BlAwbNumber != null) entry.BlAwbNumber = dto.BlAwbNumber;
        if (dto.VesselName != null) entry.VesselName = dto.VesselName;
        if (dto.Etd.HasValue) entry.Etd = dto.Etd;
        if (dto.Eta.HasValue) entry.Eta = dto.Eta;
        if (dto.ActualDeliveryDate.HasValue) entry.ActualDeliveryDate = dto.ActualDeliveryDate;
        if (dto.Remarks != null) entry.Remarks = dto.Remarks;
        if (dto.FreightForwarder != null) entry.FreightForwarder = dto.FreightForwarder;

        // ── ISO audit fields ─────────────────────────────────────────────────
        if (dto.ExpectedDeliveryDateProjectTeam.HasValue) entry.ExpectedDeliveryDateProjectTeam = dto.ExpectedDeliveryDateProjectTeam;
        if (dto.StoreNotificationDate.HasValue)           entry.StoreNotificationDate           = dto.StoreNotificationDate;
        if (dto.ExpectedDeliveryDateStoreTeam.HasValue)   entry.ExpectedDeliveryDateStoreTeam   = dto.ExpectedDeliveryDateStoreTeam;
        if (dto.ExpectedDeliveryDateAgreed.HasValue)      entry.ExpectedDeliveryDateAgreed      = dto.ExpectedDeliveryDateAgreed;
        if (dto.PaarNumber != null)                       entry.PaarNumber                      = dto.PaarNumber;
        if (dto.PaarDate.HasValue)                        entry.PaarDate                        = dto.PaarDate;
        if (dto.BlNumber != null)                         entry.BlNumber                        = dto.BlNumber;
        if (dto.AwbNumber != null)                        entry.AwbNumber                       = dto.AwbNumber;

        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entry = await db.ProjectMaterialTrackings.FindAsync(id);
        if (entry == null) return NotFound();
        db.ProjectMaterialTrackings.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Get distinct projects for filter dropdown ─────────────────────────────
    [HttpGet("projects")]
    public async Task<IEnumerable<string>> GetProjects([FromQuery] int? year)
    {
        var q = db.ProjectMaterialTrackings.AsQueryable();
        if (year.HasValue) q = q.Where(m => m.TrackingYear == year.Value);
        return await q.Where(m => m.Project != null)
            .Select(m => m.Project!)
            .Distinct().OrderBy(p => p).ToListAsync();
    }

    private static ProjectMaterialTrackingDto ToDto(ProjectMaterialTracking m) => new(
        m.Id, m.TrackingYear,
        m.PoNumber, m.PoLineItem, m.Project, m.Buyer,
        m.Description, m.Quantity, m.Supplier, m.FreightForwarder,
        m.ReadinessDate, m.PickupAuthDate, m.PickupDate,
        m.ModeOfTransport, m.FormMNumber, m.BlAwbNumber, m.VesselName,
        m.Etd, m.Eta,
        m.DeliveryStatus, m.ActualDeliveryDate,
        m.Remarks,
        m.ExpectedDeliveryDateProjectTeam, m.StoreNotificationDate,
        m.ExpectedDeliveryDateStoreTeam, m.ExpectedDeliveryDateAgreed,
        m.PaarNumber, m.PaarDate, m.BlNumber, m.AwbNumber,
        m.UpdatedAt);
}
