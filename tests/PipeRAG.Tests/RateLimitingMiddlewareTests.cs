using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PipeRAG.Api.Middleware;

namespace PipeRAG.Tests;

public class RateLimitingMiddlewareTests
{
    [Fact]
    public async Task AllowsRequests_UnderLimit()
    {
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("user1", "Free");

        await middleware.InvokeAsync(context);

        Assert.NotEqual(429, context.Response.StatusCode);
    }

    [Fact]
    public async Task Returns429_WhenLimitExceeded()
    {
        var called = false;
        var middleware = new RateLimitingMiddleware(_ => { called = true; return Task.CompletedTask; });

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
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";

        await middleware.InvokeAsync(context);

        Assert.NotEqual(429, context.Response.StatusCode);
    }

    private static HttpContext CreateContext(string userId, string tier)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        var claims = new[]
        {
            new Claim("UserId", userId),
            new Claim("Tier", tier)
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        return context;
    }
}
