using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class AutoPipelineTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _pipelineId = Guid.NewGuid();
    private readonly string _dbName = Guid.NewGuid().ToString();

    public AutoPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PipeRagDbContext>(o => o.UseInMemoryDatabase(_dbName));
        services.AddSingleton<IModelRouterService, ModelRouterService>();
        services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        services.AddSingleton<PipelineRunChannel>();
        services.AddScoped<IAutoPipelineService, AutoPipelineService>();
        services.AddSingleton<NullLoggerFactory>();
        services.AddLogging();

        _provider = services.BuildServiceProvider();

        // Seed
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PipeRagDbContext>();
        db.Users.Add(new User { Id = _userId, Email = "t@t.com", DisplayName = "T", PasswordHash = "h", Tier = UserTier.Free });
        db.Projects.Add(new Project { Id = _projectId, Name = "P", OwnerId = _userId });
        db.Pipelines.Add(new Pipeline { Id = _pipelineId, ProjectId = _projectId, Name = "Default", ConfigJson = "{}" });

        var docId = Guid.NewGuid();
        db.Documents.Add(new Document
        {
            Id = docId, ProjectId = _projectId, FileName = "test.txt",
            ContentType = "text/plain", Status = DocumentStatus.Chunked, StoragePath = "/tmp/test"
        });
        db.DocumentChunks.Add(new DocumentChunk { Id = Guid.NewGuid(), DocumentId = docId, ChunkIndex = 0, Content = "Hello world", TokenCount = 2 });
        db.DocumentChunks.Add(new DocumentChunk { Id = Guid.NewGuid(), DocumentId = docId, ChunkIndex = 1, Content = "Goodbye world", TokenCount = 2 });
        db.SaveChanges();
    }

    [Fact]
    public async Task QueueAndExecute_EmbedsChunks()
    {
        using var scope = _provider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IAutoPipelineService>();

        var runId = await sut.QueuePipelineRunAsync(_pipelineId);
        runId.Should().NotBeEmpty();

        await sut.ExecutePipelineRunAsync(runId);

        var db = scope.ServiceProvider.GetRequiredService<PipeRagDbContext>();
        var run = await db.PipelineRuns.FindAsync(runId);
        run!.Status.Should().Be(PipelineRunStatus.Completed);
        run.DocumentsProcessed.Should().Be(1);
        run.ChunksCreated.Should().Be(2);
    }

    [Fact]
    public async Task QueuePipelineRun_CreatesQueuedRun()
    {
        using var scope = _provider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IAutoPipelineService>();

        var runId = await sut.QueuePipelineRunAsync(_pipelineId);

        var db = scope.ServiceProvider.GetRequiredService<PipeRagDbContext>();
        var run = await db.PipelineRuns.FindAsync(runId);
        run.Should().NotBeNull();
        run!.Status.Should().Be(PipelineRunStatus.Queued);
    }

    public void Dispose() => _provider.Dispose();

    /// <summary>
    /// Fake embedding service that returns deterministic vectors for testing.
    /// </summary>
    private class FakeEmbeddingService : IEmbeddingService
    {
        public Task<float[]> GenerateEmbeddingAsync(string text, string modelId, CancellationToken ct = default)
        {
            var hash = text.GetHashCode();
            var embedding = new float[1536];
            embedding[0] = hash / 1000000f;
            return Task.FromResult(embedding);
        }

        public Task<IReadOnlyList<float[]>> GenerateEmbeddingsBatchAsync(
            IReadOnlyList<string> texts, string modelId, CancellationToken ct = default)
        {
            var results = texts.Select(t =>
            {
                var embedding = new float[1536];
                embedding[0] = t.GetHashCode() / 1000000f;
                return embedding;
            }).ToArray();
            return Task.FromResult<IReadOnlyList<float[]>>(results);
        }
    }
}
