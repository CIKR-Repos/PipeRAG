using PipeRAG.Core.DTOs;
using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Selects AI models based on user tier.
/// </summary>
public interface IModelRouterService
{
    /// <summary>
    /// Get the model configuration for a given user tier.
    /// </summary>
    ModelSelectionResponse GetModelsForTier(UserTier tier);
}
