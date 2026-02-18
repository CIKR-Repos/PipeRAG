namespace PipeRAG.Core.Entities;

/// <summary>
/// A chat session within a project.
/// </summary>
public class ChatSession
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
