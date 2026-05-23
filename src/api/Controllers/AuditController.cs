using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AuditLogDto>> GetAll(
        [FromQuery] string? entityType, [FromQuery] string? entityId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrEmpty(entityId)) q = q.Where(a => a.EntityId == entityId);

        return await q.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.EntityType, a.EntityId, a.Action,
                a.UserEmail, a.Timestamp, a.IpAddress, a.Notes))
            .ToListAsync();
    }
}
