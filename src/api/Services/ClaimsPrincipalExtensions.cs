using System.Security.Claims;

namespace LogisticsApi.Services;

/// <summary>
/// Tolerant readers for Entra ID claims.
///
/// Entra / Microsoft.Identity.Web surface the same logical claim under different
/// names depending on token version and whether inbound claim mapping is on.
/// Reading a single spelling caused live failures (identity resolving to null,
/// role checks silently seeing no roles), so all authorization code should read
/// claims through these helpers rather than calling FindFirstValue directly.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The caller's Entra object id, or null when the token carries none.</summary>
    public static string? GetEntraObjectId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("oid")
        ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue("sub")
        ?? principal.FindFirstValue("uid");

    /// <summary>The caller's email/UPN in lowercase, or empty string.</summary>
    public static string GetEmail(this ClaimsPrincipal principal) =>
        (principal.FindFirstValue("preferred_username")
         ?? principal.FindFirstValue("upn")
         ?? principal.FindFirstValue("email")
         ?? principal.FindFirstValue(ClaimTypes.Email)
         ?? principal.FindFirstValue(ClaimTypes.Upn)
         ?? principal.FindFirstValue("unique_name")
         ?? "").ToLowerInvariant().Trim();

    /// <summary>All app roles on the token, read under every known claim name.</summary>
    public static HashSet<string> GetAppRoles(this ClaimsPrincipal principal) =>
        principal.FindAll("roles").Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Concat(principal.FindAll("role").Select(c => c.Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the caller holds any of the given app roles.</summary>
    public static bool HasAnyRole(this ClaimsPrincipal principal, params string[] roles) =>
        principal.GetAppRoles().Overlaps(roles);

    /// <summary>
    /// True for staff who run logistics operations — they may act on records
    /// belonging to other users (approve, cancel, reassign).
    /// </summary>
    public static bool IsOperationsStaff(this ClaimsPrincipal principal) =>
        principal.HasAnyRole("Coordinator", "Manager", "Admin");

    /// <summary>True for Manager/Admin only — sign-off level actions.</summary>
    public static bool IsManagerOrAdmin(this ClaimsPrincipal principal) =>
        principal.HasAnyRole("Manager", "Admin");
}
