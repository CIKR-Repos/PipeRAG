using System.Text.Json.Serialization;

namespace PipeRAG.Core.DTOs;

/// <summary>
/// Request to create or update a pipeline.
/// </summary>
public record PipelineRequest(
    string Name,
    string? Description,
    PipelineConfigDto? Config);

/// <summary>
/// Pipeline configuration parameters.
/// </summary>
public record PipelineConfigDto(
    int ChunkSize = 512,
    int ChunkOverlap = 50,
    string EmbeddingModel = "text-embedding-3-small",
    string RetrievalStrategy = "semantic",
    int TopK = 5,
    double ScoreThreshold = 0.7);

/// <summary>
/// Pipeline response DTO.
/// </summary>
public record PipelineResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    PipelineConfigDto Config,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Pipeline run response DTO.
/// </summary>
public record PipelineRunResponse(
    Guid Id,
    Guid PipelineId,
    string Status,
    int DocumentsProcessed,
    int ChunksGenerated,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);

/// <summary>
/// Model selection based on user tier.
/// </summary>
public record ModelSelectionResponse(
    string EmbeddingModel,
    int EmbeddingDimensions,
    string ChatModel,
    int MaxTokensPerRequest,
    int MaxDocumentsPerProject);
