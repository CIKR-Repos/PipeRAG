using PipeRAG.Core.Entities;

namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Manages conversation memory with sliding window and summarization.
/// </summary>
public interface IConversationMemoryService
{
    /// <summary>
    /// Get or create a chat session.
    /// </summary>
    Task<ChatSession> GetOrCreateSessionAsync(Guid? sessionId, Guid projectId, Guid userId, string? firstMessage = null, CancellationToken ct = default);

    /// <summary>
    /// Add a message to the session.
    /// </summary>
    Task<ChatMessage> AddMessageAsync(Guid sessionId, Core.Enums.ChatMessageRole role, string content, int? tokenCount = null, CancellationToken ct = default);

    /// <summary>
    /// Get conversation history for the LLM prompt (sliding window).
    /// Returns the last N messages, with older messages summarized if needed.
    /// </summary>
    Task<List<ChatMessage>> GetConversationWindowAsync(Guid sessionId, int windowSize = 10, CancellationToken ct = default);

    /// <summary>
    /// Get all messages in a session (for history display).
    /// </summary>
    Task<List<ChatMessage>> GetAllMessagesAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Get all sessions for a project/user.
    /// </summary>
    Task<List<ChatSession>> GetSessionsAsync(Guid projectId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a session and all its messages.
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken ct = default);
}
