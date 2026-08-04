using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Services;

/// <summary>
/// Resolves the authenticated caller to a platform User record, auto-provisioning
/// one from the Entra ID token when they don't yet exist in the database.
///
/// This centralises what used to be duplicated (and inconsistent) across every
/// controller: some returned 401 when the user wasn't found, which broke the
/// very first write a newly-authenticated user attempted before the frontend's
/// auth/me call had finished creating their record.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the platform User for the given principal, creating or linking a
    /// record if necessary. Returns null only when the token carries no usable
    /// object identifier at all.
    /// </summary>
    Task<User?> ResolveOrProvisionAsync(ClaimsPrincipal principal);
}

public class CurrentUserService(AppDbContext db, ILogger<CurrentUserService> logger) : ICurrentUserService
{
    public async Task<User?> ResolveOrProvisionAsync(ClaimsPrincipal principal)
    {
        var oid = principal.FindFirstValue("oid")
               ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(oid))
            return null;

        // 1. Exact OID match — the common case for returning users.
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == oid);
        if (user != null)
            return user;

        var email = (principal.FindFirstValue("preferred_username")
                  ?? principal.FindFirstValue(ClaimTypes.Email)
                  ?? "").ToLowerInvariant().Trim();

        // 2. Pre-registered placeholder matched by email — link the real OID.
        if (!string.IsNullOrEmpty(email))
        {
            var preRegistered = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.EntraObjectId.StartsWith("pre-"));

            if (preRegistered != null)
            {
                preRegistered.EntraObjectId = oid;
                await db.SaveChangesAsync();
                return preRegistered;
            }
        }

        // 3. Auto-create, deriving the platform role from the Entra ID app roles.
        var tokenRoles = principal.FindAll("roles").Select(c => c.Value).ToList();
        var role = tokenRoles.Contains("Admin")       ? "Admin"
                 : tokenRoles.Contains("Manager")     ? "Manager"
                 : tokenRoles.Contains("Coordinator") ? "Coordinator"
                 : tokenRoles.Contains("Mechanic")    ? "Mechanic"
                 : "Driver";   // Fallback — Admin promotes manually

        var fullName = principal.FindFirstValue("name")
                    ?? principal.FindFirstValue(ClaimTypes.Name)
                    ?? email;

        var newUser = new User
        {
            Id            = Guid.NewGuid(),
            EntraObjectId = oid,
            FullName      = fullName,
            Email         = email,
            Role          = role,
            DriverStatus  = "OffDuty",
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };

        try
        {
            db.Users.Add(newUser);
            await db.SaveChangesAsync();
            logger.LogInformation("Auto-provisioned user {Email} as {Role} (OID {Oid})", email, role, oid);
            return newUser;
        }
        catch (DbUpdateException)
        {
            // Race: another concurrent request (e.g. auth/me) created the same
            // user between our lookup and insert. Detach our failed entity and
            // return the row that won the race.
            db.Entry(newUser).State = EntityState.Detached;
            return await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == oid);
        }
    }
}
