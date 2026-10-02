using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Projects and their project managers — what routes a material transport
/// request to the right first approver.
/// </summary>
[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController(AppDbContext db, IAuditService audit) : ControllerBase
{
    /// <summary>Readable by anyone signed in — the material transport form needs the list.</summary>
    [HttpGet]
    public async Task<IEnumerable<ProjectDto>> GetAll([FromQuery] bool includeInactive = false)
    {
        var q = db.Projects.Include(p => p.Manager).AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive);

        return await q.OrderBy(p => p.Name)
            .Select(p => new ProjectDto(
                p.Id, p.Name, p.ManagerUserId,
                p.Manager != null ? p.Manager.FullName : null,
                p.Manager != null ? p.Manager.Email : null,
                p.IsActive))
            .ToListAsync();
    }

    /// <summary>Assigns or changes a project's PM, or retires the project.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectDto dto)
    {
        var project = await db.Projects.Include(p => p.Manager).FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();

        var changes = new List<string>();

        if (dto.ManagerUserId.HasValue && dto.ManagerUserId != project.ManagerUserId)
        {
            var pm = await db.Users.FindAsync(dto.ManagerUserId.Value);
            if (pm == null) return BadRequest(new { error = "That user was not found." });
            if (string.IsNullOrWhiteSpace(pm.Email))
                return BadRequest(new { error = $"{pm.FullName} has no email address, so they could not be told a request is waiting." });

            changes.Add($"PM: {project.Manager?.FullName ?? "none"} → {pm.FullName}");
            project.ManagerUserId = pm.Id;
        }
        else if (dto.ClearManager == true && project.ManagerUserId != null)
        {
            changes.Add($"PM: {project.Manager?.FullName ?? "—"} → none");
            project.ManagerUserId = null;
        }

        if (dto.IsActive.HasValue && dto.IsActive.Value != project.IsActive)
        {
            changes.Add($"Active: {project.IsActive} → {dto.IsActive.Value}");
            project.IsActive = dto.IsActive.Value;
        }

        if (changes.Count == 0) return Ok(new { message = "No changes were made." });

        await db.SaveChangesAsync();
        await audit.LogAsync("Project", id.ToString(), "Updated",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"{project.Name}: {string.Join("; ", changes)}");

        return Ok(new { message = "Project updated.", changes });
    }
}
