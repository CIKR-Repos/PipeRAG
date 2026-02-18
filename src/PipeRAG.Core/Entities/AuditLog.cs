using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// Audit trail for tracking user actions.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
