using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Infrastructure.Data;
using System.Text.Json;

namespace PipeRAG.Tests;

public class PipelineCrudTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PipelineCrudTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PipeRagDbContext(options);

        // Seed user and project
        _db.Users.Add(new User { Id = _userId, Email = "test@test.com", DisplayName = "Test", PasswordHash = "hash" });
        _db.Projects.Add(new Project { Id = _projectId, Name = "Test Project", OwnerId = _userId });
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreatePipeline_StoresCorrectly()
    {
        var config = new PipelineConfigDto(ChunkSize: 1024, ChunkOverlap: 100);
        var pipeline = new Pipeline
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            Name = "My Pipeline",
            Description = "Test",
            ConfigJson = JsonSerializer.Serialize(config, JsonOpts),
            Status = PipelineStatus.Active
        };

        _db.Pipelines.Add(pipeline);
        await _db.SaveChangesAsync();

        var saved = await _db.Pipelines.FindAsync(pipeline.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("My Pipeline");
        saved.ProjectId.Should().Be(_projectId);

        var parsedConfig = JsonSerializer.Deserialize<PipelineConfigDto>(saved.ConfigJson, JsonOpts);
        parsedConfig!.ChunkSize.Should().Be(1024);
        parsedConfig.ChunkOverlap.Should().Be(100);
    }

    [Fact]
    public async Task ListPipelines_ReturnsPipelinesForProject()
    {
        _db.Pipelines.Add(new Pipeline { Id = Guid.NewGuid(), ProjectId = _projectId, Name = "P1", ConfigJson = "{}" });
        _db.Pipelines.Add(new Pipeline { Id = Guid.NewGuid(), ProjectId = _projectId, Name = "P2", ConfigJson = "{}" });
        _db.Pipelines.Add(new Pipeline { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), Name = "Other", ConfigJson = "{}" });
        await _db.SaveChangesAsync();

        var pipelines = await _db.Pipelines.Where(p => p.ProjectId == _projectId).ToListAsync();
        pipelines.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdatePipeline_UpdatesFields()
    {
        var pipeline = new Pipeline
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            Name = "Original",
            ConfigJson = "{}"
        };
        _db.Pipelines.Add(pipeline);
        await _db.SaveChangesAsync();

        pipeline.Name = "Updated";
        pipeline.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var saved = await _db.Pipelines.FindAsync(pipeline.Id);
        saved!.Name.Should().Be("Updated");
        saved.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PipelineRun_TracksStatus()
    {
        var pipeline = new Pipeline { Id = Guid.NewGuid(), ProjectId = _projectId, Name = "P", ConfigJson = "{}" };
        _db.Pipelines.Add(pipeline);

        var run = new PipelineRun
        {
            Id = Guid.NewGuid(),
            PipelineId = pipeline.Id,
            Status = PipelineRunStatus.Queued
        };
        _db.PipelineRuns.Add(run);
        await _db.SaveChangesAsync();

        run.Status = PipelineRunStatus.Running;
        run.DocumentsProcessed = 5;
        run.ChunksCreated = 42;
        await _db.SaveChangesAsync();

        run.Status = PipelineRunStatus.Completed;
        run.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var saved = await _db.PipelineRuns.FindAsync(run.Id);
        saved!.Status.Should().Be(PipelineRunStatus.Completed);
        saved.DocumentsProcessed.Should().Be(5);
        saved.ChunksCreated.Should().Be(42);
        saved.CompletedAt.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
