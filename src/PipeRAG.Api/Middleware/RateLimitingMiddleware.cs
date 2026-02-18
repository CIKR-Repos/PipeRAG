using System.Collections.Concurrent;

namespace PipeRAG.Api.Middleware;

/// <summary>
/// Per-tier rate limiting middleware using in-memory storage.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();

    private static readonly Dictionary<string, int> TierLimits = new()
    {
        ["Free"] = 100,
        ["Pro"] = 1000,
        ["Enterprise"] = 10000
    };

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the request and enforces rate limits.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip non-API routes and auth endpoints
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("UserId")?.Value;
        if (userId == null)
        {
            await _next(context);
            return;
        }

        var tier = context.User.FindFirst("Tier")?.Value ?? "Free";
        var limit = TierLimits.GetValueOrDefault(tier, 100);
        var key = $"rate:{userId}";
        var now = DateTime.UtcNow;

        var entry = _entries.GetOrAdd(key, _ => new RateLimitEntry(now));

        lock (entry)
        {
            // Reset window if hour has passed
            if ((now - entry.WindowStart).TotalHours >= 1)
            {
                entry.WindowStart = now;
                entry.Count = 0;
            }

            entry.Count++;

            if (entry.Count > limit)
            {
                var retryAfter = (int)(3600 - (now - entry.WindowStart).TotalSeconds);
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = retryAfter.ToString();
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync($"{{\"error\":\"Rate limit exceeded. Retry after {retryAfter} seconds.\"}}");
                return;
            }
        }

        await _next(context);
    }

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; }
        public int Count { get; set; }

        public RateLimitEntry(DateTime windowStart)
        {
            WindowStart = windowStart;
            Count = 0;
        }
    }
}
