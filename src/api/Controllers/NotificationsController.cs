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

    /// <summary>
    /// Broadcast a maintenance/dept notification to all Managers, Coordinators, and Admins.
    /// Used by the "Notify Departments" workflow in the Maintenance module.
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = "Coordinator,Manager,Admin,Mechanic")]
    public async Task<IActionResult> Broadcast(BroadcastNotificationDto dto)
    {
        // Target: all active Managers, Coordinators, and Admins
        var recipients = await db.Users
            .Where(u => u.IsActive && (u.Role == "Manager" || u.Role == "Coordinator" || u.Role == "Admin"))
            .Select(u => u.Id)
            .ToListAsync();

        if (recipients.Count == 0) return Ok(new { sent = 0 });

        var now = DateTime.UtcNow;
        var notifications = recipients.Select(recipientId => new Models.Entities.Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Type = dto.Type,
            Subject = dto.Title,
            Body = dto.Message,
            IsRead = false,
            Status = "Delivered",
            SentAt = now,
            RelatedEntityType = "Maintenance",
            CreatedAt = now,
        });

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();

        return Ok(new { sent = recipients.Count });
    }

    private static NotificationDto ToDto(Models.Entities.Notification n) => new(
        n.Id, n.Type, n.Subject, n.Body, n.IsRead, n.Status,
        n.SentAt, n.RelatedEntityType, n.RelatedEntityId, n.CreatedAt);
}
