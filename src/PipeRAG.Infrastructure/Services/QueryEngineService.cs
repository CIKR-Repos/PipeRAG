using System.Collections.Concurrent;
using System.Globalization;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// RAG query engine: embed query → retrieve chunks via pgvector → generate LLM response.
/// </summary>
public class QueryEngineService : IQueryEngineService
{
    private readonly PipeRagDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly IModelRouterService _modelRouter;
    private readonly IConversationMemoryService _memory;
    private readonly IConfiguration _config;
    private readonly ILogger<QueryEngineService> _logger;
    private readonly ConcurrentDictionary<string, Kernel> _chatKernelCache = new();

    public QueryEngineService(
        PipeRagDbContext db,
        IEmbeddingService embeddingService,
        IModelRouterService modelRouter,
        IConversationMemoryService memory,
        IConfiguration config,
        ILogger<QueryEngineService> logger)
    {
        _db = db;
        _embeddingService = embeddingService;
        _modelRouter = modelRouter;
        _memory = memory;
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> QueryAsync(
        Guid projectId, Guid sessionId, string userMessage, UserTier userTier,
        string retrievalStrategy = "similarity", int topK = 5, double scoreThreshold = 0.7,
        CancellationToken ct = default)
    {
        var models = _modelRouter.GetModelsForTier(userTier);

        // Store user message
        await _memory.AddMessageAsync(sessionId, ChatMessageRole.User, userMessage, ct: ct);

        // Retrieve relevant chunks
        var sources = await RetrieveChunksAsync(projectId, userMessage, models.EmbeddingModel, retrievalStrategy, topK, scoreThreshold, ct);

        // Build prompt with context and conversation history
        var conversationHistory = await _memory.GetConversationWindowAsync(sessionId, ct: ct);
        var kernel = BuildChatKernel(models.ChatModel);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = BuildChatHistory(conversationHistory, sources);
        var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
        var responseText = result.Content ?? string.Empty;
        var tokensUsed = responseText.Length / 4;

        // Store assistant message
        await _memory.AddMessageAsync(sessionId, ChatMessageRole.Assistant, responseText, tokensUsed, ct);

        return new ChatResponse(responseText, sessionId, sources, tokensUsed);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatStreamChunk> QueryStreamAsync(
        Guid projectId, Guid sessionId, string userMessage, UserTier userTier,
        string retrievalStrategy = "similarity", int topK = 5, double scoreThreshold = 0.7,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var models = _modelRouter.GetModelsForTier(userTier);

        // Store user message
        await _memory.AddMessageAsync(sessionId, ChatMessageRole.User, userMessage, ct: ct);

        // Retrieve relevant chunks
        var sources = await RetrieveChunksAsync(projectId, userMessage, models.EmbeddingModel, retrievalStrategy, topK, scoreThreshold, ct);

        // Build prompt
        var conversationHistory = await _memory.GetConversationWindowAsync(sessionId, ct: ct);
        var kernel = BuildChatKernel(models.ChatModel);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = BuildChatHistory(conversationHistory, sources);

        var fullResponse = new StringBuilder();

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: ct))
        {
            if (chunk.Content is not null)
            {
                fullResponse.Append(chunk.Content);
                yield return new ChatStreamChunk(chunk.Content, false, sessionId);
            }
        }

        // Store assistant message
        var fullResponseText = fullResponse.ToString();
        var tokensUsed = fullResponseText.Length / 4;
        await _memory.AddMessageAsync(sessionId, ChatMessageRole.Assistant, fullResponseText, tokensUsed, ct);

        // Final chunk with sources
        yield return new ChatStreamChunk(string.Empty, true, sessionId, sources, tokensUsed);
    }

    private async Task<List<SourceReference>> RetrieveChunksAsync(
        Guid projectId, string query, string embeddingModel,
        string strategy, int topK, double scoreThreshold, CancellationToken ct)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, embeddingModel, ct);
        var embeddingStr = "[" + string.Join(",", queryEmbedding.Select(v => v.ToString(CultureInfo.InvariantCulture))) + "]";

        // Use raw SQL for pgvector cosine distance search
        var sql = @"
            SELECT dc.""Id"", dc.""DocumentId"", dc.""Content"", dc.""ChunkIndex"",
                   d.""FileName"" AS ""DocumentName"",
                   (dc.""Embedding"" <=> @p0::vector) AS ""Distance""
            FROM ""DocumentChunks"" dc
            INNER JOIN ""Documents"" d ON dc.""DocumentId"" = d.""Id""
            WHERE d.""ProjectId"" = @p1
              AND dc.""Embedding"" IS NOT NULL
              AND (dc.""Embedding"" <=> @p0::vector) < @p2
            ORDER BY ""Distance""
            LIMIT @p3";

        var sources = new List<SourceReference>();

        try
        {
            var conn = _db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync(ct);
            try
            {
                using var command = conn.CreateCommand();
                command.CommandText = sql;

            var pEmbedding = command.CreateParameter();
            pEmbedding.ParameterName = "@p0";
            pEmbedding.Value = embeddingStr;
            command.Parameters.Add(pEmbedding);

            var pProject = command.CreateParameter();
            pProject.ParameterName = "@p1";
            pProject.Value = projectId;
            command.Parameters.Add(pProject);

            var pThreshold = command.CreateParameter();
            pThreshold.ParameterName = "@p2";
            pThreshold.Value = 1.0 - scoreThreshold; // cosine distance = 1 - similarity
            command.Parameters.Add(pThreshold);

            var pLimit = command.CreateParameter();
            pLimit.ParameterName = "@p3";
            pLimit.Value = topK;
            command.Parameters.Add(pLimit);

            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                sources.Add(new SourceReference(
                    DocumentId: reader.GetGuid(1),
                    DocumentName: reader.GetString(4),
                    ChunkContent: reader.GetString(2),
                    Score: 1.0 - reader.GetDouble(5)
                ));
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector search failed, returning empty results. This is expected with InMemory provider.");
        }

        // For hybrid strategy, also do keyword search and merge
        if (strategy.Equals("hybrid", StringComparison.OrdinalIgnoreCase) && sources.Count < topK)
        {
            var keywordResults = await _db.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document.ProjectId == projectId && c.Content.Contains(query))
                .Take(topK - sources.Count)
                .ToListAsync(ct);

            foreach (var chunk in keywordResults)
            {
                if (sources.All(s => s.DocumentId != chunk.DocumentId || s.ChunkContent != chunk.Content))
                {
                    sources.Add(new SourceReference(chunk.DocumentId, chunk.Document.FileName, chunk.Content, 0.5));
                }
            }
        }

        return sources;
    }

    private ChatHistory BuildChatHistory(List<Core.Entities.ChatMessage> conversation, List<SourceReference> sources)
    {
        var history = new ChatHistory();

        // System prompt
        var contextBlock = sources.Count > 0
            ? $"\n\nRelevant context from documents:\n{string.Join("\n---\n", sources.Select(s => $"[{s.DocumentName}]: {s.ChunkContent}"))}"
            : "";

        history.AddSystemMessage(
            $"You are a helpful AI assistant answering questions based on the user's documents. " +
            $"Use the provided context to answer accurately. If the context doesn't contain relevant information, say so.{contextBlock}");

        // Add conversation history (excluding the current message which we already added)
        foreach (var msg in conversation)
        {
            if (msg.Role == ChatMessageRole.System)
                history.AddSystemMessage(msg.Content);
            else if (msg.Role == ChatMessageRole.User)
                history.AddUserMessage(msg.Content);
            else if (msg.Role == ChatMessageRole.Assistant)
                history.AddAssistantMessage(msg.Content);
        }

        return history;
    }

    private Kernel BuildChatKernel(string modelId)
    {
        return _chatKernelCache.GetOrAdd(modelId, id =>
        {
            var apiKey = _config["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:ApiKey not configured.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(id, apiKey);
            return builder.Build();
        });
    }
}
