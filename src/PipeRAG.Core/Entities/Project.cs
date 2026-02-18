namespace PipeRAG.Core.Entities;

/// <summary>
/// A RAG project containing documents and pipelines.
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User Owner { get; set; } = null!;
    public Organization? Organization { get; set; }
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Pipeline> Pipelines { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
}
