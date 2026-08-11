using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Called by the frontend on every login.
/// Resolves the current Entra ID token to a platform User record.
///
/// Flow:
///   1. Look up by EntraObjectId  → exact match, return immediately.
///   2. Look up by email          → pre-registered user (placeholder OID),
///                                  update their OID to the real Entra OID and return.
///   3. Neither found             → auto-create a minimal record so the person
///                                  can at least see the app (role defaults to Driver
///                                  so admins can promote them later).
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(AppDbContext db, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Resolves (and provisions if needed) the caller's platform record.
    /// Uses the shared resolver so identity resolution is identical everywhere
    /// and tolerant of Entra claim-name variations.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var resolved = await currentUser.ResolveOrProvisionAsync(User);
        if (resolved == null)
            return Unauthorized(new { error = "Cannot resolve user identity from token" });

        return ToDto(resolved);
    }

    /// <summary>
    /// Diagnostic: lists the claims present on the caller's token. Useful when
    /// identity resolution fails and we need to see what Entra actually sent.
    /// </summary>
    [HttpGet("claims")]
    public ActionResult<object> Claims() => Ok(new
    {
        claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
    });

    [HttpGet("me-legacy")]
    public async Task<ActionResult<UserDto>> MeLegacy()
    {
        var entraOid = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(entraOid)) return Unauthorized();

        var email = (User.FindFirstValue("preferred_username")
                  ?? User.FindFirstValue(ClaimTypes.Email)
                  ?? "").ToLowerInvariant().Trim();

        var fullName = User.FindFirstValue("name")
                    ?? User.FindFirstValue(ClaimTypes.Name)
                    ?? email;

        // ── 1. Exact OID match ────────────────────────────────────────────────
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraOid);
        if (user != null)
            return ToDto(user);

        // ── 2. Email match (pre-registered placeholder) ───────────────────────
        if (!string.IsNullOrEmpty(email))
        {
            var preRegistered = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.EntraObjectId.StartsWith("pre-"));

            if (preRegistered != null)
            {
                // Link their real Entra OID — they now authenticate correctly
                preRegistered.EntraObjectId = entraOid;
                preRegistered.FullName = fullName; // update name from token
                await db.SaveChangesAsync();
                return ToDto(preRegistered);
            }
        }

        // ── 3. Auto-create (first login by someone not pre-registered) ────────
        // Use the Entra ID app roles from the token to set the correct platform
        // role immediately, instead of always defaulting to Driver.
        var tokenRoles = User.FindAll("roles").Select(c => c.Value).ToList();
        var assignedRole = tokenRoles.Contains("Admin")       ? "Admin"
                         : tokenRoles.Contains("Manager")     ? "Manager"
                         : tokenRoles.Contains("Coordinator") ? "Coordinator"
                         : tokenRoles.Contains("Mechanic")    ? "Mechanic"
                         : "Driver";   // Fallback — Admin promotes manually

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = entraOid,
            FullName = fullName,
            Email = email,
            Role = assignedRole,
            DriverStatus = "OffDuty",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            db.Users.Add(newUser);
            await db.SaveChangesAsync();
            return ToDto(newUser);
        }
        catch (DbUpdateException)
        {
            // Race condition: another request created the same user — just fetch it
            var existing = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraOid);
            return existing == null ? StatusCode(500) : ToDto(existing);
        }
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role,
        u.DriverStatus, u.LicenceNo, u.LicenceExpiry, u.IsActive, u.LastStatusChange);
}
