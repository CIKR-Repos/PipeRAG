using PipeRAG.Core.DTOs;
using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Performs RAG query: embed query → retrieve chunks → generate LLM response.
/// </summary>
public interface IQueryEngineService
{
    /// <summary>
    /// Execute a RAG query and return a complete response.
    /// </summary>
    Task<ChatResponse> QueryAsync(
        Guid projectId,
        Guid sessionId,
        string userMessage,
        UserTier userTier,
        string retrievalStrategy = "similarity",
        int topK = 5,
        double scoreThreshold = 0.7,
        CancellationToken ct = default);

    /// <summary>
    /// Execute a RAG query and stream response tokens via an async enumerable.
    /// </summary>
    IAsyncEnumerable<ChatStreamChunk> QueryStreamAsync(
        Guid projectId,
        Guid sessionId,
        string userMessage,
        UserTier userTier,
        string retrievalStrategy = "similarity",
        int topK = 5,
        double scoreThreshold = 0.7,
        CancellationToken ct = default);
}
