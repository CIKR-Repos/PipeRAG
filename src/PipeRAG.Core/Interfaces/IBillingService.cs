using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Interfaces;

public interface IBillingService
{
    Task<string> CreateCheckoutSessionAsync(Guid userId, UserTier tier, string successUrl, string cancelUrl);
    Task<string> CreatePortalSessionAsync(Guid userId, string returnUrl);
    Task HandleWebhookAsync(string json, string stripeSignature);
    Task<SubscriptionDto?> GetSubscriptionAsync(Guid userId);
}

public record SubscriptionDto(
    UserTier Tier,
    string Status,
    DateTime? CurrentPeriodEnd,
    string? StripeCustomerId);
