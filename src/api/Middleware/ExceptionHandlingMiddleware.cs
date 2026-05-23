using System.Net;
using System.Text.Json;

namespace LogisticsApi.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static async Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = ex switch
        {
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
            ArgumentException or InvalidOperationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var body = JsonSerializer.Serialize(new
        {
            error = ex switch
            {
                KeyNotFoundException => "Not found",
                UnauthorizedAccessException => "Forbidden",
                ArgumentException or InvalidOperationException => ex.Message,
                _ => "An unexpected error occurred"
            }
        });

        await ctx.Response.WriteAsync(body);
    }
}
