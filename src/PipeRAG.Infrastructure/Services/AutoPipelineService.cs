using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pgvector;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Orchestrates the auto-pipeline: chunk → embed → store vectors.
/// </summary>
public class AutoPipelineService : IAutoPipelineService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoPipelineService> _logger;
    private readonly PipelineRunChannel _channel;

    public AutoPipelineService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutoPipelineService> logger,
        PipelineRunChannel channel)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _channel = channel;
    }

    /// <inheritdoc />
    public async Task<Guid> QueuePipelineRunAsync(Guid pipelineId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PipeRagDbContext>();

        var pipeline = await db.Pipelines.FindAsync([pipelineId], ct)
            ?? throw new InvalidOperationException($"Pipeline {pipelineId} not found.");

        var run = new Core.Entities.PipelineRun
        {
            Id = Guid.NewGuid(),
            PipelineId = pipelineId,
            Status = PipelineRunStatus.Queued,
            StartedAt = DateTime.UtcNow
        };

        db.PipelineRuns.Add(run);
        await db.SaveChangesAsync(ct);

        await _channel.Writer.WriteAsync(run.Id, ct);
        return run.Id;
    }

    /// <inheritdoc />
    public async Task ExecutePipelineRunAsync(Guid runId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PipeRagDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var modelRouter = scope.ServiceProvider.GetRequiredService<IModelRouterService>();

        var run = await db.PipelineRuns
            .Include(r => r.Pipeline)
            .FirstOrDefaultAsync(r => r.Id == runId, ct);

        if (run is null) return;

        try
        {
            run.Status = PipelineRunStatus.Running;
            await db.SaveChangesAsync(ct);

            var pipeline = run.Pipeline;
            var config = JsonSerializer.Deserialize<PipelineConfigDto>(pipeline.ConfigJson)
                ?? new PipelineConfigDto();

            // Get project owner's tier for model selection
            var project = await db.Projects.Include(p => p.Owner).FirstAsync(p => p.Id == pipeline.ProjectId, ct);
            var models = modelRouter.GetModelsForTier(project.Owner.Tier);
            var embeddingModel = config.EmbeddingModel ?? models.EmbeddingModel;

            // Find all un-embedded chunks for this project's documents
            // Note: For InMemory provider, Embedding is ignored, so we check document status instead
            var chunks = await db.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document.ProjectId == pipeline.ProjectId
                    && c.Document.Status != DocumentStatus.Embedded)
                .OrderBy(c => c.DocumentId).ThenBy(c => c.ChunkIndex)
                .ToListAsync(ct);

            var docsProcessed = new HashSet<Guid>();
            var chunksGenerated = 0;
            const int batchSize = 20;

            for (var i = 0; i < chunks.Count; i += batchSize)
            {
                ct.ThrowIfCancellationRequested();
                var batch = chunks.Skip(i).Take(batchSize).ToList();
                var texts = batch.Select(c => c.Content).ToList();

                var embeddings = await embeddingService.GenerateEmbeddingsBatchAsync(texts, embeddingModel, ct);

                for (var j = 0; j < batch.Count; j++)
                {
                    batch[j].Embedding = new Vector(embeddings[j]);
                    docsProcessed.Add(batch[j].DocumentId);
                    chunksGenerated++;
                }

                await db.SaveChangesAsync(ct);
            }

            // Update document statuses
            var docIds = docsProcessed.ToList();
            var docs = await db.Documents.Where(d => docIds.Contains(d.Id)).ToListAsync(ct);
            foreach (var doc in docs)
                doc.Status = DocumentStatus.Embedded;

            run.DocumentsProcessed = docsProcessed.Count;
            run.ChunksCreated = chunksGenerated;
            run.Status = PipelineRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Pipeline run {RunId} completed: {Docs} docs, {Chunks} chunks",
                runId, docsProcessed.Count, chunksGenerated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline run {RunId} failed", runId);
            run.Status = PipelineRunStatus.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
