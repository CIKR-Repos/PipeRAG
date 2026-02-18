namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Generates vector embeddings for text content.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for the given text using the specified model.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, string modelId, CancellationToken ct = default);

    /// <summary>
    /// Generate embeddings for multiple texts in batch.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsBatchAsync(IReadOnlyList<string> texts, string modelId, CancellationToken ct = default);
}
