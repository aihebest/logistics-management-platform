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
            if (byEmail != null) return byEmail;

            // Synthesise a stable id from the email so repeat logins map to the
            // same record; a later login carrying a real oid will relink it.
            oid = $"email-{email}";
        }

        // 1. Exact OID match — the common case for returning users.
        var user = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == oid);
        if (user != null)
            return user;

        // 1b. Known email but a different/placeholder stored id — relink to the
        //     current token's id so future lookups hit the fast path above.
        if (!string.IsNullOrEmpty(email))
        {
            var existingByEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingByEmail != null)
            {
                existingByEmail.EntraObjectId = oid;
                await db.SaveChangesAsync();
                return existingByEmail;
            }
        }

        // 2. Auto-create, deriving the platform role from the Entra ID app roles.
        //    Role claims also vary by mapping config, so read both spellings.
        var tokenRoles = principal.FindAll("roles").Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToList();
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
