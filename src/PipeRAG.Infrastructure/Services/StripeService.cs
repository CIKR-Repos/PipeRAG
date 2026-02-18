using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using Stripe;
using Stripe.Checkout;

namespace PipeRAG.Infrastructure.Services;

public class StripeService : IBillingService
{
    private readonly PipeRagDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;

    public StripeService(PipeRagDbContext db, IConfiguration config, ILogger<StripeService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid userId, UserTier tier, string successUrl, string cancelUrl)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        string customerId;

        if (sub?.StripeCustomerId is { Length: > 0 })
        {
            customerId = sub.StripeCustomerId;
        }
        else
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
            });
            customerId = customer.Id;
        }

        var priceId = tier switch
        {
            UserTier.Pro => _config["Stripe:ProPriceId"],
            UserTier.Enterprise => _config["Stripe:EnterprisePriceId"],
            _ => throw new ArgumentException("Cannot checkout for Free tier")
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["tier"] = tier.ToString()
            }
        });

        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(Guid userId, string returnUrl)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new InvalidOperationException("No subscription found");

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = sub.StripeCustomerId,
            ReturnUrl = returnUrl
        });

        return session.Url;
    }

    public async Task HandleWebhookAsync(string json, string stripeSignature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];
        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

        _logger.LogInformation("Stripe webhook: {Type}", stripeEvent.Type);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutCompleted(stripeEvent);
                break;
            case EventTypes.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdated(stripeEvent);
                break;
            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent);
                break;
        }
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid userId)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (sub == null)
        {
            var user = await _db.Users.FindAsync(userId);
            return user == null ? null : new SubscriptionDto(user.Tier, "active", null, null);
        }
        return new SubscriptionDto(sub.Tier, sub.Status.ToString().ToLower(), sub.CurrentPeriodEnd, sub.StripeCustomerId);
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var userIdStr = session.Metadata.GetValueOrDefault("userId");
        var tierStr = session.Metadata.GetValueOrDefault("tier");
        if (!Guid.TryParse(userIdStr, out var userId) || !Enum.TryParse<UserTier>(tierStr, out var tier))
            return;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (sub == null)
        {
            sub = new Core.Entities.Subscription { UserId = userId };
            _db.Subscriptions.Add(sub);
        }

        sub.StripeCustomerId = session.CustomerId;
        sub.StripeSubscriptionId = session.SubscriptionId;
        sub.Tier = tier;
        sub.Status = SubscriptionStatus.Active;
        sub.UpdatedAt = DateTime.UtcNow;

        var user = await _db.Users.FindAsync(userId);
        if (user != null) user.Tier = tier;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Checkout completed: user {UserId} -> {Tier}", userId, tier);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSub) return;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);
        if (sub == null) return;

        sub.Status = stripeSub.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            "incomplete" => SubscriptionStatus.Incomplete,
            "trialing" => SubscriptionStatus.Trialing,
            _ => SubscriptionStatus.Active
        };
        sub.CurrentPeriodStart = stripeSub.Items?.Data?.FirstOrDefault()?.CurrentPeriodStart ?? DateTime.UtcNow;
        sub.CurrentPeriodEnd = stripeSub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd ?? DateTime.UtcNow.AddMonths(1);
        sub.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSub) return;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);
        if (sub == null) return;

        sub.Status = SubscriptionStatus.Cancelled;
        sub.CancelledAt = DateTime.UtcNow;
        sub.UpdatedAt = DateTime.UtcNow;

        var user = await _db.Users.FindAsync(sub.UserId);
        if (user != null) user.Tier = UserTier.Free;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Subscription cancelled: user {UserId} -> Free", sub.UserId);
    }
}
