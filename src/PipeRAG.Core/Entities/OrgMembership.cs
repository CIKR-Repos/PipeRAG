using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// Join entity between User and Organization.
/// </summary>
public class OrgMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public OrgRole Role { get; set; } = OrgRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
