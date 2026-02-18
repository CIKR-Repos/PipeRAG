using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// An uploaded document for RAG processing.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public int TokenCount { get; set; }
    public int ChunkCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
