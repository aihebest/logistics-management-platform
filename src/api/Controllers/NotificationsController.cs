using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<NotificationDto>> GetMine()
    {
        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (user == null) return [];

        return await db.Notifications
            .Where(n => n.RecipientId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => ToDto(n))
            .ToListAsync();
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var n = await db.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        n.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (user == null) return NoContent();

        await db.Notifications
            .Where(n => n.RecipientId == user.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return NoContent();
    }

    private static NotificationDto ToDto(Models.Entities.Notification n) => new(
        n.Id, n.Type, n.Subject, n.Body, n.IsRead, n.Status,
        n.SentAt, n.RelatedEntityType, n.RelatedEntityId, n.CreatedAt);
}
