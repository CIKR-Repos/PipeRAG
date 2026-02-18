using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public UserTier Tier { get; set; } = UserTier.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public User User { get; set; } = null!;
}
