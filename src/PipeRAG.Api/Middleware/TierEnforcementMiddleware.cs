using System.Security.Claims;
using PipeRAG.Core.Interfaces;

namespace PipeRAG.Api.Middleware;

public class TierEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TierEnforcementMiddleware> _logger;

    private static readonly HashSet<string> QueryPaths = ["/api/chat"];
    private static readonly HashSet<string> DocumentPaths = ["/api/documents"];
    private static readonly HashSet<string> ProjectPaths = ["/api/projects"];

    public TierEnforcementMiddleware(RequestDelegate next, ILogger<TierEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUsageTrackingService usageService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        if (method != "POST" || !context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        // Check query limits
        if (QueryPaths.Any(p => path.StartsWith(p)))
        {
            if (!await usageService.CanPerformQueryAsync(userId))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new { error = "Daily query limit reached. Upgrade your plan." });
                return;
            }
        }

        // Check document limits
        if (DocumentPaths.Any(p => path.StartsWith(p)))
        {
            if (!await usageService.CanCreateDocumentAsync(userId))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new { error = "Document limit reached. Upgrade your plan." });
                return;
            }
        }

        // Check project limits
        if (ProjectPaths.Any(p => path.StartsWith(p)))
        {
            if (!await usageService.CanCreateProjectAsync(userId))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new { error = "Project limit reached. Upgrade your plan." });
                return;
            }
        }

        await _next(context);
    }
}
