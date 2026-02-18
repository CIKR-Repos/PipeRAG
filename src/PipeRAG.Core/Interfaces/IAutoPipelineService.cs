namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Automatically processes documents through the RAG pipeline (chunk → embed → index).
/// </summary>
public interface IAutoPipelineService
{
    /// <summary>
    /// Queue a pipeline run for background processing.
    /// </summary>
    Task<Guid> QueuePipelineRunAsync(Guid pipelineId, CancellationToken ct = default);

    /// <summary>
    /// Execute a pipeline run: embed all un-embedded chunks for the pipeline's project.
    /// </summary>
    Task ExecutePipelineRunAsync(Guid runId, CancellationToken ct = default);
}
