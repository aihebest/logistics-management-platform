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
        // Entra / Microsoft.Identity.Web can surface the object id under several
        // claim names depending on token version and whether inbound claim
        // mapping is enabled. Check every known variant, then fall back to the
        // subject claim, so identity resolution never depends on one spelling.
        var oid = principal.FindFirstValue("oid")
               ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue("sub")
               ?? principal.FindFirstValue("uid");

        var email = (principal.FindFirstValue("preferred_username")
                  ?? principal.FindFirstValue("upn")
                  ?? principal.FindFirstValue("email")
                  ?? principal.FindFirstValue(ClaimTypes.Email)
                  ?? principal.FindFirstValue(ClaimTypes.Upn)
                  ?? principal.FindFirstValue("unique_name")
                  ?? "").ToLowerInvariant().Trim();

        // If no object id is present at all, fall back to identifying the user by
        // email. Without either we genuinely cannot identify the caller.
        if (string.IsNullOrEmpty(oid))
        {
            if (string.IsNullOrEmpty(email))
            {
                logger.LogError(
                    "Cannot resolve caller: token carried no object-id or email claim. Claims present: {Claims}",
                    string.Join(", ", principal.Claims.Select(c => c.Type)));
                return null;
            }

            logger.LogWarning(
                "Token had no object-id claim; falling back to email identity for {Email}. Claims present: {Claims}",
                email, string.Join(", ", principal.Claims.Select(c => c.Type)));

            var byEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (byEmail != null) return await SyncRoleAsync(byEmail, principal);

            // Synthesise a stable id from the email so repeat logins map to the
            // same record; a later login carrying a real oid will relink it.
            oid = $"email-{email}";
        }

        // 1. Exact OID match — the common case for returning users.
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == oid);
        if (user != null)
            return await SyncRoleAsync(user, principal);

        // 1b. Known email but a different/placeholder stored id — relink to the
        //     current token's id so future lookups hit the fast path above.
        if (!string.IsNullOrEmpty(email))
        {
            var existingByEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingByEmail != null)
            {
                existingByEmail.EntraObjectId = oid;
                await db.SaveChangesAsync();
                return await SyncRoleAsync(existingByEmail, principal);
            }
        }

        // 2. Auto-create with the role from the Entra ID app roles.
        var role = RoleFromToken(principal);

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
            // Only actual drivers carry a duty status.
            DriverStatus  = role == "Driver" ? "OffDuty" : null,
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

    /// <summary>
    /// Maps the caller's Entra ID app roles onto a platform role.
    ///
    /// Someone with no app role assigned is <c>Staff</c> — they can raise
    /// requests (which needs no role) but must NOT land in the Drivers list.
    /// Defaulting these users to "Driver" previously filled the fleet roster
    /// with office staff.
    /// </summary>
    private static string RoleFromToken(ClaimsPrincipal principal)
    {
        var roles = principal.GetAppRoles();
        return roles.Contains("Admin")       ? "Admin"
             : roles.Contains("Manager")     ? "Manager"
             // HOD approves material transport at stage 1, before GM Logistics.
             : roles.Contains("HOD")         ? "HOD"
             : roles.Contains("Coordinator") ? "Coordinator"
             : roles.Contains("Mechanic")    ? "Mechanic"
             : roles.Contains("Driver")      ? "Driver"
             : "Staff";
    }

    /// <summary>
    /// Keeps the stored role in step with Entra on every sign-in.
    ///
    /// Role used to be written only at creation, so promoting someone in Entra
    /// (e.g. to Manager) never reached the database and their permissions never
    /// changed. Users with no app role are left alone, so a driver registered
    /// manually by a coordinator isn't demoted to Staff on their next login.
    /// </summary>
    private async Task<User> SyncRoleAsync(User user, ClaimsPrincipal principal)
    {
        var roles = principal.GetAppRoles();
        if (roles.Count == 0) return user;      // nothing authoritative to apply

        var desired = RoleFromToken(principal);
        if (desired == "Staff" || desired == user.Role) return user;

        logger.LogInformation("Role sync for {Email}: {Old} → {New}", user.Email, user.Role, desired);
        user.Role = desired;
        if (desired == "Driver") user.DriverStatus ??= "OffDuty";
        await db.SaveChangesAsync();
        return user;
    }
}
