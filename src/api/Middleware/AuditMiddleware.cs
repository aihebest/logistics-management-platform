using System.Security.Claims;
using LogisticsApi.Services;

namespace LogisticsApi.Middleware;

public class AuditMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> _auditMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext ctx, IAuditService audit)
    {
        await next(ctx);

        if (!_auditMethods.Contains(ctx.Request.Method)) return;
        if (!ctx.User.Identity?.IsAuthenticated ?? true) return;
        if (ctx.Response.StatusCode < 200 || ctx.Response.StatusCode >= 300) return;

        var userId = ctx.User.FindFirstValue("oid") ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var email = ctx.User.FindFirstValue("preferred_username") ?? ctx.User.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        var path = ctx.Request.Path.Value ?? "";
        var segments = path.Trim('/').Split('/');

        // Derive entity type from URL: /api/vehicles/... → "Vehicle"
        var entity = segments.Length >= 2
            ? char.ToUpper(segments[1][0]) + segments[1][1..].TrimEnd('s')
            : "Unknown";

        var entityId = segments.Length >= 3 ? segments[2] : "";

        var action = ctx.Request.Method switch
        {
            "POST"   => "Created",
            "PUT"    => "Updated",
            "PATCH"  => "Patched",
            "DELETE" => "Deleted",
            _        => ctx.Request.Method
        };

        await audit.LogAsync(entity, entityId, action, userId, email,
            ctx.Connection.RemoteIpAddress?.ToString());
    }
}
