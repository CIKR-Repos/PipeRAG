namespace PipeRAG.Core.Entities;

/// <summary>
/// Represents an organization that groups users and projects.
/// </summary>
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<OrgMembership> Members { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
