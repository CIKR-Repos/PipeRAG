using PipeRAG.Core.DTOs;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Routes to appropriate AI models based on user tier.
/// </summary>
public class ModelRouterService : IModelRouterService
{
    /// <inheritdoc />
    public ModelSelectionResponse GetModelsForTier(UserTier tier) => tier switch
    {
        UserTier.Free => new ModelSelectionResponse(
            EmbeddingModel: "text-embedding-3-small",
            EmbeddingDimensions: 1536,
            ChatModel: "gpt-4.1-mini",
            MaxTokensPerRequest: 4096,
            MaxDocumentsPerProject: 50),

        UserTier.Pro => new ModelSelectionResponse(
            EmbeddingModel: "text-embedding-3-large",
            EmbeddingDimensions: 3072,
            ChatModel: "gpt-4.1",
            MaxTokensPerRequest: 8192,
            MaxDocumentsPerProject: 500),

        UserTier.Enterprise => new ModelSelectionResponse(
            EmbeddingModel: "text-embedding-3-large",
            EmbeddingDimensions: 3072,
            ChatModel: "gpt-4.1",
            MaxTokensPerRequest: 16384,
            MaxDocumentsPerProject: 5000),

        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };
}
