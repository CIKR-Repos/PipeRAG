using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// A single message in a chat session.
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TokenCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatSession Session { get; set; } = null!;
}
