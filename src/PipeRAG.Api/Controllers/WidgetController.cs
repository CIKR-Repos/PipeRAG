using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Infrastructure.Data;
using System.Security.Claims;

namespace PipeRAG.Api.Controllers;

/// <summary>
/// Manages widget configuration for a project (authenticated).
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/widget")]
[Authorize]
public class WidgetController : ControllerBase
{
    private readonly PipeRagDbContext _db;

    public WidgetController(PipeRagDbContext db) => _db = db;

    /// <summary>
    /// Get the widget configuration for a project.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<WidgetConfigResponse>> Get(Guid projectId, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == projectId, ct);
        if (config is null) return NotFound(new { error = "Widget not configured." });

        return Ok(MapToResponse(config));
    }

    /// <summary>
    /// Create or update the widget configuration for a project.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<WidgetConfigResponse>> Upsert(Guid projectId, [FromBody] WidgetConfigRequest request, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == projectId, ct);
        if (config is null)
        {
            config = new WidgetConfig { ProjectId = projectId };
            _db.WidgetConfigs.Add(config);
        }

        if (request.PrimaryColor is not null) config.PrimaryColor = request.PrimaryColor;
        if (request.BackgroundColor is not null) config.BackgroundColor = request.BackgroundColor;
        if (request.TextColor is not null) config.TextColor = request.TextColor;
        if (request.Position is not null) config.Position = request.Position;
        if (request.AvatarUrl is not null) config.AvatarUrl = request.AvatarUrl;
        if (request.Title is not null) config.Title = request.Title;
        if (request.Subtitle is not null) config.Subtitle = request.Subtitle;
        if (request.PlaceholderText is not null) config.PlaceholderText = request.PlaceholderText;
        if (request.AllowedOrigins is not null) config.AllowedOrigins = request.AllowedOrigins;
        if (request.IsActive.HasValue) config.IsActive = request.IsActive.Value;
        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(MapToResponse(config));
    }

    /// <summary>
    /// Delete the widget configuration for a project.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid projectId, CancellationToken ct)
    {
        var project = await GetAuthorizedProjectAsync(projectId, ct);
        if (project is null) return NotFound(new { error = "Project not found." });

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == projectId, ct);
        if (config is null) return NotFound(new { error = "Widget not configured." });

        _db.WidgetConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<Project?> GetAuthorizedProjectAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([projectId], ct);
        if (project is null || project.OwnerId != GetUserId()) return null;
        return project;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static WidgetConfigResponse MapToResponse(WidgetConfig c) => new(
        c.Id, c.ProjectId, c.PrimaryColor, c.BackgroundColor, c.TextColor,
        c.Position, c.AvatarUrl, c.Title, c.Subtitle, c.PlaceholderText,
        c.AllowedOrigins, c.IsActive, c.CreatedAt, c.UpdatedAt);
}
