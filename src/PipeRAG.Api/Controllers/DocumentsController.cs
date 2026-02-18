using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using System.Security.Claims;

namespace PipeRAG.Api.Controllers;

/// <summary>
/// Manages document uploads, processing, and chunk previews.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly PipeRagDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IDocumentProcessor _processor;
    private readonly IChunkingService _chunking;
    private readonly ILogger<DocumentsController> _logger;

    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv"
    };

    private static readonly Dictionary<UserTier, long> MaxFileSizeByTier = new()
    {
        [UserTier.Free] = 50L * 1024 * 1024,
        [UserTier.Pro] = 200L * 1024 * 1024,
        [UserTier.Enterprise] = 500L * 1024 * 1024
    };

    public DocumentsController(
        PipeRagDbContext db,
        IFileStorageService storage,
        IDocumentProcessor processor,
        IChunkingService chunking,
        ILogger<DocumentsController> logger)
    {
        _db = db;
        _storage = storage;
        _processor = processor;
        _chunking = chunking;
        _logger = logger;
    }

    /// <summary>
    /// Upload one or more documents to a project.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(Guid projectId, List<IFormFile> files, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null)
            return NotFound(new { error = "Project not found." });

        var userId = GetUserId();
        if (project.OwnerId != userId)
            return Forbid();

        var user = await _db.Users.FindAsync([userId], ct);
        var tier = user?.Tier ?? UserTier.Free;
        var maxSize = MaxFileSizeByTier[tier];

        var results = new List<DocumentResponse>();
        var failedCount = 0;

        foreach (var file in files)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName);
                if (!ExtensionToContentType.TryGetValue(ext, out var contentType))
                {
                    failedCount++;
                    continue;
                }

                if (file.Length > maxSize)
                {
                    failedCount++;
                    continue;
                }

                var doc = new Document
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    FileName = file.FileName,
                    ContentType = contentType,
                    FileSizeBytes = file.Length,
                    Status = DocumentStatus.Processing
                };

                // Save file
                await using var stream = file.OpenReadStream();
                doc.StoragePath = await _storage.SaveFileAsync(projectId, doc.Id, file.FileName, stream, ct);
                _db.Documents.Add(doc);
                await _db.SaveChangesAsync(ct);

                // Process: extract text + chunk
                try
                {
                    await using var readStream = await _storage.GetFileAsync(doc.StoragePath, ct);
                    var text = await _processor.ExtractTextAsync(readStream, contentType, ct);
                    var chunks = _chunking.ChunkText(text);

                    doc.TokenCount = _chunking.EstimateTokenCount(text);
                    doc.ChunkCount = chunks.Count;
                    doc.Status = DocumentStatus.Chunked;
                    doc.ProcessedAt = DateTime.UtcNow;

                    foreach (var chunk in chunks)
                    {
                        _db.DocumentChunks.Add(new DocumentChunk
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = doc.Id,
                            ChunkIndex = chunk.Index,
                            Content = chunk.Content,
                            TokenCount = chunk.TokenCount
                        });
                    }

                    await _db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process document {DocumentId}", doc.Id);
                    doc.Status = DocumentStatus.Failed;
                    await _db.SaveChangesAsync(ct);
                }

                results.Add(ToResponse(doc));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}", file.FileName);
                failedCount++;
            }
        }

        return Ok(new DocumentUploadResponse(results, files.Count, results.Count, failedCount));
    }

    /// <summary>
    /// List all documents in a project.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentResponse>>> List(Guid projectId, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var docs = await _db.Documents
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return Ok(docs.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Get document details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentResponse>> Get(Guid projectId, Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.ProjectId == projectId, ct);
        if (doc is null) return NotFound(new { error = "Document not found." });
        return Ok(ToResponse(doc));
    }

    /// <summary>
    /// Delete a document and its chunks and file.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents.Include(d => d.Chunks).FirstOrDefaultAsync(d => d.Id == id && d.ProjectId == projectId, ct);
        if (doc is null) return NotFound(new { error = "Document not found." });

        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null || project.OwnerId != GetUserId())
            return Forbid();

        // Delete file from storage
        try { await _storage.DeleteFileAsync(doc.StoragePath, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete file for document {DocumentId}", id); }

        _db.DocumentChunks.RemoveRange(doc.Chunks);
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Get paginated chunk preview for a document.
    /// </summary>
    [HttpGet("{documentId:guid}/chunks")]
    public async Task<ActionResult<ChunkPreviewResponse>> GetChunks(
        Guid projectId, Guid documentId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var docExists = await _db.Documents.AnyAsync(d => d.Id == documentId && d.ProjectId == projectId, ct);
        if (!docExists) return NotFound(new { error = "Document not found." });

        var query = _db.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex);

        var totalCount = await query.CountAsync(ct);
        var chunks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new DocumentChunkResponse(c.Id, c.ChunkIndex, c.Content, c.TokenCount))
            .ToListAsync(ct);

        return Ok(new ChunkPreviewResponse(chunks, totalCount, page, pageSize));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static DocumentResponse ToResponse(Document doc) => new(
        doc.Id, doc.ProjectId, doc.FileName, doc.ContentType,
        doc.FileSizeBytes, doc.Status.ToString(), doc.TokenCount,
        doc.ChunkCount, doc.CreatedAt, doc.ProcessedAt);
}
