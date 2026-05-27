using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/project-materials")]
[Authorize]
public class ProjectMaterialTrackingController(AppDbContext db) : ControllerBase
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
        var entry = new ProjectMaterialTracking
        {
            Id = Guid.NewGuid(),
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
            DeliveryStatus = "Pending",
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
        m.Remarks, m.UpdatedAt);
}
