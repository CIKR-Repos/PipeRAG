using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;

namespace PipeRAG.Api.Controllers;

[ApiController]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billing;
    private readonly IUsageTrackingService _usage;

    public BillingController(IBillingService billing, IUsageTrackingService usage)
    {
        _billing = billing;
        _usage = usage;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user identifier");
        return userId;
    }

    [Authorize]
    [HttpPost("create-checkout-session")]
    public async Task<ActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
    {
        if (!Enum.TryParse<UserTier>(request.Tier, true, out var tier) || tier == UserTier.Free)
            return BadRequest(new { error = "Invalid tier" });

        var url = await _billing.CreateCheckoutSessionAsync(
            GetUserId(), tier, request.SuccessUrl, request.CancelUrl);
        return Ok(new { url });
    }

    [Authorize]
    [HttpPost("create-portal-session")]
    public async Task<ActionResult> CreatePortalSession([FromBody] PortalRequest request)
    {
        var url = await _billing.CreatePortalSessionAsync(GetUserId(), request.ReturnUrl);
        return Ok(new { url });
    }

    [HttpPost("webhook")]
    public async Task<ActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        try
        {
            await _billing.HandleWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("subscription")]
    public async Task<ActionResult> GetSubscription()
    {
        var sub = await _billing.GetSubscriptionAsync(GetUserId());
        return Ok(sub);
    }

    [Authorize]
    [HttpGet("usage")]
    public async Task<ActionResult> GetUsage()
    {
        var usage = await _usage.GetUsageAsync(GetUserId());
        return Ok(usage);
    }
}

public record CheckoutRequest(string Tier, string SuccessUrl, string CancelUrl);
public record PortalRequest(string ReturnUrl);
