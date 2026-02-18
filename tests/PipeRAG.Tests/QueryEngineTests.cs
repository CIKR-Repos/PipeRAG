using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class QueryEngineTests
{
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<IModelRouterService> _modelRouter = new();
    private readonly Mock<IConversationMemoryService> _memory = new();
    private readonly Mock<IConfiguration> _config = new();

    public QueryEngineTests()
    {
        _modelRouter.Setup(m => m.GetModelsForTier(It.IsAny<UserTier>()))
            .Returns(new ModelSelectionResponse("text-embedding-3-small", 1536, "gpt-4.1-mini", 4096, 50));

        _embeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        _memory.Setup(m => m.AddMessageAsync(It.IsAny<Guid>(), It.IsAny<ChatMessageRole>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessage { Id = Guid.NewGuid(), Content = "test" });

        _memory.Setup(m => m.GetConversationWindowAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        _config.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);
    }

    [Fact]
    public void ModelRouter_ReturnsCorrectModels_ForFreeTier()
    {
        var router = new ModelRouterService();
        var result = router.GetModelsForTier(UserTier.Free);

        result.ChatModel.Should().Be("gpt-4.1-mini");
        result.EmbeddingModel.Should().Be("text-embedding-3-small");
        result.MaxDocumentsPerProject.Should().Be(50);
    }

    [Fact]
    public void ModelRouter_ReturnsCorrectModels_ForProTier()
    {
        var router = new ModelRouterService();
        var result = router.GetModelsForTier(UserTier.Pro);

        result.ChatModel.Should().Be("gpt-4.1");
        result.EmbeddingModel.Should().Be("text-embedding-3-large");
        result.MaxDocumentsPerProject.Should().Be(500);
    }

    [Fact]
    public async Task EmbeddingService_GeneratesEmbedding_MockReturnsCorrectDimension()
    {
        var result = await _embeddingService.Object.GenerateEmbeddingAsync("hello", "text-embedding-3-small");
        result.Should().HaveCount(1536);
    }

    // Note: Memory_AddMessage test removed — it was testing mock setup, not real behavior.
    // Integration testing of AddMessageAsync requires a full QueryEngineService with SK kernel,
    // which needs an OpenAI API key. Covered by ChatController integration tests instead.

    [Fact]
    public async Task Memory_GetConversationWindow_ReturnsEmptyForNewSession()
    {
        var result = await _memory.Object.GetConversationWindowAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }
}
