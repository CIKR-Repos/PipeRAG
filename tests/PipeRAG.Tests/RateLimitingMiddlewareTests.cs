using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using PipeRAG.Api.Middleware;

namespace PipeRAG.Tests;

public class RateLimitingMiddlewareTests : IDisposable
{
    private readonly IMemoryCache cache;

    public RateLimitingMiddlewareTests()
    {
        cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task AllowsRequests_UnderLimit()
    {
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask, cache);
        var context = CreateContext("user1", "Free");

        await middleware.InvokeAsync(context);

        Assert.NotEqual(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task Returns429_WhenLimitExceeded()
    {
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask, cache);

        // Exceed Free tier limit (100)
        var userId = Guid.NewGuid().ToString();
        for (int i = 0; i < 101; i++)
        {
            var ctx = CreateContext(userId, "Free");
            await middleware.InvokeAsync(ctx);
        }

        var finalCtx = CreateContext(userId, "Free");
        await middleware.InvokeAsync(finalCtx);

        Assert.Equal(429, finalCtx.Response.StatusCode);
        Assert.True(finalCtx.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public async Task SkipsNonApiRoutes()
    {
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask, cache);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        await middleware.InvokeAsync(context);

        Assert.NotEqual(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task RejectsRequest_ShortCircuitsPipeline()
    {
        var called = false;
        var middleware = new RateLimitingMiddleware(_ => { called = true; return Task.CompletedTask; }, cache);

        var userId = Guid.NewGuid().ToString();
        // Exhaust Free tier limit (100)
        for (int i = 0; i < 101; i++)
        {
            var ctx = CreateContext(userId, "Free");
            await middleware.InvokeAsync(ctx);
        }

        // Next request should be rejected and pipeline NOT called
        called = false;
        var finalCtx = CreateContext(userId, "Free");
        await middleware.InvokeAsync(finalCtx);

        Assert.Equal(429, finalCtx.Response.StatusCode);
        Assert.False(called); // pipeline was short-circuited
    }

    [Fact]
    public async Task AuthRoutes_SkipRateLimiting()
    {
        var called = false;
        var middleware = new RateLimitingMiddleware(_ => { called = true; return Task.CompletedTask; }, cache);
        var context = CreateContext("user1", "Free", "/api/auth/login");

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequests_NotRateLimited()
    {
        var called = false;
        var middleware = new RateLimitingMiddleware(_ => { called = true; return Task.CompletedTask; }, cache);
        var context = CreateContext(null, "Free", "/api/data");

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.NotEqual(429, context.Response.StatusCode);
    }

    private static HttpContext CreateContext(string? userId, string tier, string path = "/api/test")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (userId != null)
        {
            var claims = new[]
            {
                new Claim("UserId", userId),
                new Claim("Tier", tier)
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        return context;
    }

    public void Dispose() => cache.Dispose();
}
