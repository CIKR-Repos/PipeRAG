using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.Entities;
using PipeRAG.Infrastructure.Data;
using System.Security.Claims;

namespace PipeRAG.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly PipeRagDbContext _db;

    public ProjectController(PipeRagDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = GetUserId();
        var projects = await _db.Projects
            .Where(p => p.OwnerId == userId)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                DocumentCount = p.Documents.Count,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var project = await _db.Projects
            .Where(p => p.Id == id && p.OwnerId == userId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                DocumentCount = p.Documents.Count,
                p.CreatedAt,
                p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);
        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        var userId = GetUserId();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        return Ok(new { project.Id, project.Name, project.Description, DocumentCount = 0, project.CreatedAt, project.UpdatedAt });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
        return Guid.Parse(claim);
    }
}

public record CreateProjectRequest(string Name, string? Description);
