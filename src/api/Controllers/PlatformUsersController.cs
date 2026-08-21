using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Platform user administration.
///
/// Exists to solve a chicken-and-egg problem in the approval chain: the platform
/// could only notify people who had already signed in, but the whole purpose of
/// the notification is to tell them something is waiting. HODs and managers who
/// had been given a role in Entra ID were invisible to the notification service
/// until they happened to log in.
///
/// Pre-registering a colleague creates their record immediately, so they receive
/// emails straight away. On their first sign-in the account is matched by email
/// and linked to their real Entra identity — see CurrentUserService.
/// </summary>
[ApiController]
[Route("api/platform-users")]
[Authorize(Roles = "Manager,Admin")]
public class PlatformUsersController(
    AppDbContext db,
    IAuditService audit,
    ILogger<PlatformUsersController> logger) : ControllerBase
{
    private static readonly string[] ValidRoles =
        ["Admin", "Manager", "HOD", "Coordinator", "Mechanic", "Driver", "Staff"];

    [HttpGet]
    public async Task<IEnumerable<UserDto>> GetAll([FromQuery] string? role, [FromQuery] bool? pendingOnly)
    {
        var q = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            q = q.Where(u => u.Role == role);

        // Pre-registered but never signed in — still carrying a placeholder id.
        if (pendingOnly == true)
            q = q.Where(u => u.EntraObjectId.StartsWith("pre-"));

        return await q
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => ToDto(u))
            .ToListAsync();
    }

    /// <summary>Pre-registers a colleague so notifications can reach them immediately.</summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Register(RegisterPlatformUserDto dto)
    {
        var email = dto.Email?.ToLowerInvariant().Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "An email address is required — it is how notifications reach them and how their account links on first login." });

        if (!ValidRoles.Contains(dto.Role))
            return BadRequest(new { error = $"Role must be one of: {string.Join(", ", ValidRoles)}" });

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
        {
            // Already known — treat this as a role correction rather than an error,
            // which is almost always what the admin actually intends.
            var previousRole = existing.Role;
            existing.Role     = dto.Role;
            existing.IsActive = true;
            if (!string.IsNullOrWhiteSpace(dto.FullName))    existing.FullName    = dto.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) existing.PhoneNumber = dto.PhoneNumber.Trim();
            if (dto.Role == "Driver") existing.DriverStatus ??= "OffDuty";

            await db.SaveChangesAsync();
            await audit.LogAsync("User", existing.Id.ToString(), "RoleChanged",
                User.GetEntraObjectId() ?? "", User.GetEmail(), null,
                $"{existing.FullName} ({email}): {previousRole} → {dto.Role}");

            return Ok(ToDto(existing));
        }

        var user = new User
        {
            Id            = Guid.NewGuid(),
            // Placeholder until they sign in; CurrentUserService swaps in the real
            // Entra object id once it sees a matching email.
            EntraObjectId = $"pre-{Guid.NewGuid():N}",
            FullName      = dto.FullName.Trim(),
            Email         = email,
            PhoneNumber   = dto.PhoneNumber?.Trim(),
            Role          = dto.Role,
            DriverStatus  = dto.Role == "Driver" ? "OffDuty" : null,
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        await audit.LogAsync("User", user.Id.ToString(), "PreRegistered",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"{user.FullName} ({email}) pre-registered as {user.Role}");

        logger.LogInformation("Pre-registered {Email} as {Role}", email, user.Role);
        return Ok(ToDto(user));
    }

    /// <summary>Corrects a user's details, role, or active status.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePlatformUserDto dto)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(dto.Role) && dto.Role != user.Role)
        {
            if (!ValidRoles.Contains(dto.Role))
                return BadRequest(new { error = $"Role must be one of: {string.Join(", ", ValidRoles)}" });

            changes.Add($"Role: {user.Role} → {dto.Role}");
            user.Role = dto.Role;
            if (dto.Role == "Driver") user.DriverStatus ??= "OffDuty";
            else if (user.Role != "Driver") user.DriverStatus = null;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var email = dto.Email.ToLowerInvariant().Trim();
            if (email != user.Email)
            {
                if (await db.Users.AnyAsync(u => u.Email == email && u.Id != id))
                    return Conflict(new { error = $"Another user already has the email {email}." });
                changes.Add($"Email: {user.Email} → {email}");
                user.Email = email;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName) && dto.FullName.Trim() != user.FullName)
        {
            changes.Add($"Name: {user.FullName} → {dto.FullName.Trim()}");
            user.FullName = dto.FullName.Trim();
        }

        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber.Trim();

        if (dto.IsActive.HasValue && dto.IsActive.Value != user.IsActive)
        {
            changes.Add($"Active: {user.IsActive} → {dto.IsActive.Value}");
            user.IsActive = dto.IsActive.Value;
        }

        if (changes.Count == 0) return Ok(new { message = "No changes were made." });

        await db.SaveChangesAsync();
        await audit.LogAsync("User", id.ToString(), "Updated",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            string.Join("; ", changes));

        return Ok(new { message = "User updated.", changes });
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role,
        u.DriverStatus, u.LicenceNo, u.LicenceExpiry, u.IsActive, u.LastStatusChange);
}
