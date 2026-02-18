namespace PipeRAG.Core.DTOs;

/// <summary>
/// Response returned after uploading documents.
/// </summary>
public record DocumentUploadResponse(List<DocumentResponse> Documents, int TotalFiles, int SuccessCount, int FailedCount);

/// <summary>
/// Response representing a single document.
/// </summary>
public record DocumentResponse(
    Guid Id,
    Guid ProjectId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    int TokenCount,
    int ChunkCount,
    DateTime CreatedAt,
    DateTime? ProcessedAt);

/// <summary>
/// Response representing a document chunk.
/// </summary>
public record DocumentChunkResponse(Guid Id, int ChunkIndex, string Content, int TokenCount);

/// <summary>
/// Request for paginated chunk preview.
/// </summary>
public record ChunkPreviewRequest(int Page = 1, int PageSize = 20);

/// <summary>
/// Paginated response for chunk preview.
/// </summary>
public record ChunkPreviewResponse(List<DocumentChunkResponse> Chunks, int TotalCount, int Page, int PageSize);
