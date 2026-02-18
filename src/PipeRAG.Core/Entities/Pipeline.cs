using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// A RAG pipeline configuration.
/// </summary>
public class Pipeline
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public PipelineStatus Status { get; set; } = PipelineStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
    public ICollection<PipelineRun> Runs { get; set; } = [];
}
