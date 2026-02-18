using Pgvector;

namespace PipeRAG.Core.Entities;

/// <summary>
/// A chunk of a document with its vector embedding for semantic search.
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }

    /// <summary>
    /// Vector embedding (1536 dimensions for OpenAI ada-002).
    /// </summary>
    public Vector? Embedding { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
}
