using System.Text.Json;
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
/// Pipeline configuration and run management.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/pipelines")]
[Authorize]
public class PipelineController : ControllerBase
{
    private readonly PipeRagDbContext _db;
    private readonly IAutoPipelineService _autoPipeline;
    private readonly IModelRouterService _modelRouter;
    private readonly ILogger<PipelineController> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PipelineController(
        PipeRagDbContext db,
        IAutoPipelineService autoPipeline,
        IModelRouterService modelRouter,
        ILogger<PipelineController> logger)
    {
        _db = db;
        _autoPipeline = autoPipeline;
        _modelRouter = modelRouter;
        _logger = logger;
    }

    /// <summary>
    /// Create a new pipeline configuration for a project.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PipelineResponse>> Create(Guid projectId, [FromBody] PipelineRequest request, CancellationToken ct)
    {
        if (GetUserId() is null) return Unauthorized();

        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var config = request.Config ?? new PipelineConfigDto();
        var pipeline = new Pipeline
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name,
            Description = request.Description,
            ConfigJson = JsonSerializer.Serialize(config, JsonOpts),
            Status = PipelineStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Pipelines.Add(pipeline);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { projectId, id = pipeline.Id }, ToResponse(pipeline));
    }

    /// <summary>
    /// List all pipelines for a project.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PipelineResponse>>> List(Guid projectId, CancellationToken ct)
    {
        if (GetUserId() is null) return Unauthorized();

        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var pipelines = await _db.Pipelines
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        // Auto-create default pipeline if none exist
        if (pipelines.Count == 0)
        {
            var defaultPipeline = new Pipeline
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = "Default Pipeline",
                Description = "Auto-created default pipeline",
                ConfigJson = JsonSerializer.Serialize(new PipelineConfigDto(), JsonOpts),
                Status = PipelineStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _db.Pipelines.Add(defaultPipeline);
            await _db.SaveChangesAsync(ct);
            pipelines = [defaultPipeline];
        }

        return Ok(pipelines.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Get pipeline details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PipelineResponse>> Get(Guid projectId, Guid id, CancellationToken ct)
    {
        if (GetUserId() is null) return Unauthorized();

        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var pipeline = await _db.Pipelines.FirstOrDefaultAsync(p => p.Id == id && p.ProjectId == projectId, ct);
        if (pipeline is null) return NotFound(new { error = "Pipeline not found." });

        return Ok(ToResponse(pipeline));
    }

    /// <summary>
    /// Update pipeline configuration.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PipelineResponse>> Update(Guid projectId, Guid id, [FromBody] PipelineRequest request, CancellationToken ct)
    {
        if (GetUserId() is null) return Unauthorized();

        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var pipeline = await _db.Pipelines.FirstOrDefaultAsync(p => p.Id == id && p.ProjectId == projectId, ct);
        if (pipeline is null) return NotFound(new { error = "Pipeline not found." });

        pipeline.Name = request.Name;
        pipeline.Description = request.Description;
        if (request.Config is not null)
            pipeline.ConfigJson = JsonSerializer.Serialize(request.Config, JsonOpts);
        pipeline.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(pipeline));
    }

    /// <summary>
    /// Trigger a pipeline run.
    /// </summary>
    [HttpPost("{pipelineId:guid}/run")]
    public async Task<ActionResult<PipelineRunResponse>> TriggerRun(Guid projectId, Guid pipelineId, CancellationToken ct)
    {
        if (GetUserId() is null) return Unauthorized();

        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var pipeline = await _db.Pipelines.FirstOrDefaultAsync(p => p.Id == pipelineId && p.ProjectId == projectId, ct);
        if (pipeline is null) return NotFound(new { error = "Pipeline not found." });

        var runId = await _autoPipeline.QueuePipelineRunAsync(pipelineId, ct);
        var run = await _db.PipelineRuns.FindAsync([runId], ct);

        return Accepted(ToRunResponse(run!));
    }

    /// <summary>
    /// List pipeline runs.
    /// </summary>
    [HttpGet("{pipelineId:guid}/runs")]
    public async Task<ActionResult<List<PipelineRunResponse>>> ListRuns(Guid projectId, Guid pipelineId, CancellationToken ct)
    {
        if (GetUserId() is null) return Unauthorized();

        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var pipelineExists = await _db.Pipelines.AnyAsync(p => p.Id == pipelineId && p.ProjectId == projectId, ct);
        if (!pipelineExists) return NotFound(new { error = "Pipeline not found." });

        var runs = await _db.PipelineRuns
            .Where(r => r.PipelineId == pipelineId)
            .OrderByDescending(r => r.QueuedAt)
            .ToListAsync(ct);

        return Ok(runs.Select(ToRunResponse).ToList());
    }

    /// <summary>
    /// Get model selection for the current user's tier.
    /// </summary>
    [HttpGet("/api/models")]
    public async Task<ActionResult<ModelSelectionResponse>> GetModels(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var user = await _db.Users.FindAsync([userId.Value], ct);
        if (user is null) return Unauthorized();

        return Ok(_modelRouter.GetModelsForTier(user.Tier));
    }

    private async Task<Project?> GetAuthorizedProjectAsync(Guid projectId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return null;

        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null || project.OwnerId != userId.Value)
            return null;
        return project;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static PipelineResponse ToResponse(Pipeline p)
    {
        var config = JsonSerializer.Deserialize<PipelineConfigDto>(p.ConfigJson, JsonOpts) ?? new PipelineConfigDto();
        return new PipelineResponse(p.Id, p.ProjectId, p.Name, p.Description, config, p.Status.ToString(), p.CreatedAt, p.UpdatedAt);
    }

    private static PipelineRunResponse ToRunResponse(PipelineRun r) =>
        new(r.Id, r.PipelineId, r.Status.ToString(), r.DocumentsProcessed, r.ChunksCreated, r.QueuedAt, r.StartedAt, r.CompletedAt, r.ErrorMessage);
}
