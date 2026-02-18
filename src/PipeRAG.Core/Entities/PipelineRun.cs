using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Entities;

/// <summary>
/// A single execution of a pipeline.
/// </summary>
public class PipelineRun
{
    public Guid Id { get; set; }
    public Guid PipelineId { get; set; }
    public PipelineRunStatus Status { get; set; } = PipelineRunStatus.Queued;
    public int DocumentsProcessed { get; set; }
    public int ChunksCreated { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Pipeline Pipeline { get; set; } = null!;
}
