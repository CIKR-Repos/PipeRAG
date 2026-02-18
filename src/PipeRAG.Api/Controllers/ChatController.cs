using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using System.Security.Claims;

namespace PipeRAG.Api.Controllers;

/// <summary>
/// Handles chat interactions and session management for RAG queries.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly PipeRagDbContext _db;
    private readonly IQueryEngineService _queryEngine;
    private readonly IConversationMemoryService _memory;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        PipeRagDbContext db,
        IQueryEngineService queryEngine,
        IConversationMemoryService memory,
        ILogger<ChatController> logger)
    {
        _db = db;
        _queryEngine = queryEngine;
        _memory = memory;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message and get an AI response.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(Guid projectId, [FromBody] ChatRequest request, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var userId = GetUserId();
        var user = await _db.Users.FindAsync([userId], ct);
        var tier = user?.Tier ?? UserTier.Free;

        var session = await _memory.GetOrCreateSessionAsync(request.SessionId, projectId, userId, request.Message, ct);

        var response = await _queryEngine.QueryAsync(
            projectId, session.Id, request.Message, tier,
            request.RetrievalStrategy, request.TopK, ct: ct);

        return Ok(response);
    }

    /// <summary>
    /// Send a chat message and receive a streaming SSE response.
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream(Guid projectId, [FromBody] ChatRequest request, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null)
        {
            Response.StatusCode = 404;
            return;
        }

        var userId = GetUserId();
        var user = await _db.Users.FindAsync([userId], ct);
        var tier = user?.Tier ?? UserTier.Free;

        var session = await _memory.GetOrCreateSessionAsync(request.SessionId, projectId, userId, request.Message, ct);

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await foreach (var chunk in _queryEngine.QueryStreamAsync(
            projectId, session.Id, request.Message, tier,
            request.RetrievalStrategy, request.TopK, ct: ct))
        {
            var json = JsonSerializer.Serialize(chunk, jsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    /// <summary>
    /// List all chat sessions for a project.
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<ChatSessionResponse>>> GetSessions(Guid projectId, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var sessions = await _memory.GetSessionsAsync(projectId, GetUserId(), ct);

        var response = sessions.Select(s => new ChatSessionResponse(
            s.Id, s.Title, s.Messages.Count, s.CreatedAt, s.LastMessageAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get chat history for a session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<List<ChatMessageResponse>>> GetMessages(Guid projectId, Guid sessionId, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        // Verify session belongs to user
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.ProjectId == projectId && s.UserId == GetUserId(), ct);
        if (session is null) return NotFound(new { error = "Session not found." });

        var messages = await _memory.GetAllMessagesAsync(sessionId, ct);

        var response = messages.Select(m => new ChatMessageResponse(
            m.Id, m.Role.ToString(), m.Content, null, m.TokenCount, m.CreatedAt
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Delete a chat session and all its messages.
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid projectId, Guid sessionId, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var deleted = await _memory.DeleteSessionAsync(sessionId, GetUserId(), ct);
        if (!deleted) return NotFound(new { error = "Session not found." });

        return NoContent();
    }

    private async Task<Core.Entities.Project?> GetAuthorizedProjectAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null || project.OwnerId != GetUserId())
            return null;
        return project;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
