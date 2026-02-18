using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// Represents an application user.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserTier Tier { get; set; } = UserTier.Free;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<OrgMembership> OrgMemberships { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public Subscription? Subscription { get; set; }
    public ICollection<UsageRecord> UsageRecords { get; set; } = [];
}
