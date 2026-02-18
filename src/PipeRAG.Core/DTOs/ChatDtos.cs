namespace PipeRAG.Core.DTOs;

/// <summary>
/// Request to send a chat message.
/// </summary>
public record ChatRequest(
    string Message,
    Guid? SessionId = null,
    string RetrievalStrategy = "similarity",
    int TopK = 5);

/// <summary>
/// Response from a chat query.
/// </summary>
public record ChatResponse(
    string Message,
    Guid SessionId,
    List<SourceReference> Sources,
    int TokensUsed);

/// <summary>
/// A chunk sent during SSE streaming.
/// </summary>
public record ChatStreamChunk(
    string Content,
    bool Done,
    Guid SessionId,
    List<SourceReference>? Sources = null,
    int? TokensUsed = null);

/// <summary>
/// Summary of a chat session.
/// </summary>
public record ChatSessionResponse(
    Guid Id,
    string? Title,
    int MessageCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// A single chat message in history.
/// </summary>
public record ChatMessageResponse(
    Guid Id,
    string Role,
    string Content,
    List<SourceReference>? Sources,
    int? TokensUsed,
    DateTime CreatedAt);

/// <summary>
/// Reference to a source document chunk used for retrieval.
/// </summary>
public record SourceReference(
    Guid DocumentId,
    string DocumentName,
    string ChunkContent,
    double Score);
