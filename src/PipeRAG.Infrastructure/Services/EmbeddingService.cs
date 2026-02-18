using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;
using PipeRAG.Core.Interfaces;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Generates embeddings using Semantic Kernel's text embedding generation.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly ConcurrentDictionary<string, Kernel> _kernelCache = new();

    public EmbeddingService(IConfiguration config, ILogger<EmbeddingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, string modelId, CancellationToken ct = default)
    {
        var kernel = _kernelCache.GetOrAdd(modelId, BuildKernel);
        var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var result = await embeddingService.GenerateEmbeddingAsync(text, kernel, ct);
        return result.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsBatchAsync(
        IReadOnlyList<string> texts, string modelId, CancellationToken ct = default)
    {
        var kernel = _kernelCache.GetOrAdd(modelId, BuildKernel);
        var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var results = await embeddingService.GenerateEmbeddingsAsync(texts.ToList(), kernel, ct);
        return results.Select(r => r.ToArray()).ToArray();
    }

    private Kernel BuildKernel(string modelId)
    {
        var apiKey = _config["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey not configured.");

        var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010
        builder.AddOpenAITextEmbeddingGeneration(modelId, apiKey);
#pragma warning restore SKEXP0010
        return builder.Build();
    }
}
