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

    private static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role,
        u.DriverStatus, u.LicenceNo, u.LicenceExpiry, u.IsActive, u.LastStatusChange);
}
