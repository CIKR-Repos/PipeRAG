using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Manages conversation memory with sliding window and summarization.
/// </summary>
public class ConversationMemoryService : IConversationMemoryService
{
    private readonly PipeRagDbContext _db;
    private readonly ILogger<ConversationMemoryService> _logger;

    public ConversationMemoryService(PipeRagDbContext db, ILogger<ConversationMemoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChatSession> GetOrCreateSessionAsync(
        Guid? sessionId, Guid projectId, Guid userId, string? firstMessage = null, CancellationToken ct = default)
    {
        if (sessionId.HasValue)
        {
            var existing = await _db.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.UserId == userId, ct);
            if (existing is not null) return existing;
        }

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Title = firstMessage is not null
                ? (firstMessage.Length > 60 ? firstMessage[..60] + "..." : firstMessage)
                : "New Chat",
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    /// <inheritdoc />
    public async Task<ChatMessage> AddMessageAsync(
        Guid sessionId, ChatMessageRole role, string content, int? tokenCount = null, CancellationToken ct = default)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            TokenCount = tokenCount,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);

        // Update session's LastMessageAt
        var session = await _db.ChatSessions.FindAsync([sessionId], ct);
        if (session is not null)
            session.LastMessageAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return message;
    }

    /// <inheritdoc />
    public async Task<List<ChatMessage>> GetConversationWindowAsync(
        Guid sessionId, int windowSize = 10, CancellationToken ct = default)
    {
        var totalMessages = await _db.ChatMessages
            .CountAsync(m => m.SessionId == sessionId, ct);

        if (totalMessages <= windowSize)
        {
            return await _db.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);
        }

        // Get the last windowSize messages
        var recentMessages = await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(windowSize)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        // Get older messages for summarization
        var olderMessages = await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Take(totalMessages - windowSize)
            .ToListAsync(ct);

        if (olderMessages.Count > 0)
        {
            // Create a summary message to prepend
            var summaryText = BuildSummary(olderMessages);
            // Synthetic summary message (not persisted) — uses new Guid to avoid entity conflicts
            var summaryMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = ChatMessageRole.System,
                Content = summaryText,
                CreatedAt = recentMessages[0].CreatedAt.AddSeconds(-1)
            };

            var result = new List<ChatMessage> { summaryMessage };
            result.AddRange(recentMessages);
            return result;
        }

        return recentMessages;
    }

    /// <inheritdoc />
    public async Task<List<ChatMessage>> GetAllMessagesAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ChatSession>> GetSessionsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return await _db.ChatSessions
            .Where(s => s.ProjectId == projectId && s.UserId == userId)
            .Include(s => s.Messages)
            .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        if (session is null) return false;

        _db.ChatSessions.Remove(session);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static string BuildSummary(List<ChatMessage> messages)
    {
        var parts = messages
            .Where(m => m.Role != ChatMessageRole.System)
            .Select(m => $"{m.Role}: {(m.Content.Length > 100 ? m.Content[..100] + "..." : m.Content)}");

        return $"Summary of earlier conversation:\n{string.Join("\n", parts)}";
    }
}
