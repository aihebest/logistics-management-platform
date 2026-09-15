using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Departments and their heads.
///
/// The head is held here rather than on the user record because one person can
/// head more than one department. This is also what routes travel requests for
/// verification, so a department left without a head is a gap worth surfacing.
/// </summary>
[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(
    AppDbContext db,
    IAuditService audit,
    ILogger<DepartmentsController> logger) : ControllerBase
{
    /// <summary>
    /// Readable by any signed-in user — the travel request form needs the list
    /// to populate its department field.
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<DepartmentDto>> GetAll([FromQuery] bool includeInactive = false)
    {
        var q = db.Departments.Include(d => d.Hod).AsQueryable();
        if (!includeInactive) q = q.Where(d => d.IsActive);

        return await q.OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Name,
                d.HodUserId,
                d.Hod != null ? d.Hod.FullName : null,
                d.Hod != null ? d.Hod.Email : null,
                d.IsActive,
                d.Members.Count(m => m.IsActive)))
            .ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<DepartmentDto>> Create(CreateDepartmentDto dto)
    {
        var name = dto.Name.Trim();
        if (name.Length == 0) return BadRequest(new { error = "A department name is required." });

        if (await db.Departments.AnyAsync(d => d.Name == name))
            return BadRequest(new { error = $"'{name}' already exists." });

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            HodUserId = dto.HodUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync();

        await audit.LogAsync("Department", department.Id.ToString(), "Created",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null, name);

        return CreatedAtAction(nameof(GetAll), new { id = department.Id }, await ToDtoAsync(department.Id));
    }

    /// <summary>
    /// Assigns or moves a headship, renames a department, or retires one.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateDepartmentDto dto)
    {
        var department = await db.Departments.Include(d => d.Hod).FirstOrDefaultAsync(d => d.Id == id);
        if (department == null) return NotFound();

        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != department.Name)
        {
            var name = dto.Name.Trim();
            if (await db.Departments.AnyAsync(d => d.Id != id && d.Name == name))
                return BadRequest(new { error = $"'{name}' already exists." });

            changes.Add($"Name: {department.Name} → {name}");
            department.Name = name;
        }

        if (dto.HodUserId.HasValue)
        {
            var hod = await db.Users.FindAsync(dto.HodUserId.Value);
            if (hod == null) return BadRequest(new { error = "That user was not found." });

            // A head who cannot be emailed cannot be told there is something to
            // verify, which is the whole point of the assignment.
            if (string.IsNullOrWhiteSpace(hod.Email))
                return BadRequest(new
                {
                    error = $"{hod.FullName} has no email address on the platform, so they could not be " +
                            "notified of requests to verify. Add their email under Platform Users first."
                });

            if (hod.Role != "HOD" && hod.Role != "Admin")
                logger.LogWarning(
                    "{Email} was made head of {Department} but holds the role {Role}, not HOD",
                    hod.Email, department.Name, hod.Role);

            changes.Add($"Head: {department.Hod?.FullName ?? "none"} → {hod.FullName}");
            department.HodUserId = hod.Id;
        }
        else if (dto.ClearHod == true)
        {
            changes.Add($"Head: {department.Hod?.FullName ?? "none"} → none");
            department.HodUserId = null;
        }

        if (dto.IsActive.HasValue && dto.IsActive.Value != department.IsActive)
        {
            changes.Add($"Active: {department.IsActive} → {dto.IsActive.Value}");
            department.IsActive = dto.IsActive.Value;
        }

        if (changes.Count == 0) return Ok(new { message = "No changes were made." });

        await db.SaveChangesAsync();

        await audit.LogAsync("Department", id.ToString(), "Updated",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null, string.Join("; ", changes));

        return Ok(new { message = "Department updated.", changes });
    }

    private async Task<DepartmentDto> ToDtoAsync(Guid id)
    {
        var d = await db.Departments.Include(x => x.Hod).FirstAsync(x => x.Id == id);
        return new DepartmentDto(
            d.Id, d.Name, d.HodUserId, d.Hod?.FullName, d.Hod?.Email, d.IsActive,
            await db.Users.CountAsync(u => u.DepartmentId == d.Id && u.IsActive));
    }
}
