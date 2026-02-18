using FluentAssertions;
using PipeRAG.Core.Enums;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class ModelRouterTests
{
    private readonly ModelRouterService _sut = new();

    [Fact]
    public void FreeTier_ReturnsSmallEmbeddingModel()
    {
        var result = _sut.GetModelsForTier(UserTier.Free);

        result.EmbeddingModel.Should().Be("text-embedding-3-small");
        result.EmbeddingDimensions.Should().Be(1536);
        result.ChatModel.Should().Be("gpt-4.1-mini");
        result.MaxDocumentsPerProject.Should().Be(50);
    }

    [Fact]
    public void ProTier_ReturnsLargeEmbeddingModel()
    {
        var result = _sut.GetModelsForTier(UserTier.Pro);

        result.EmbeddingModel.Should().Be("text-embedding-3-large");
        result.EmbeddingDimensions.Should().Be(3072);
        result.ChatModel.Should().Be("gpt-4.1");
        result.MaxDocumentsPerProject.Should().Be(500);
    }

    [Fact]
    public void EnterpriseTier_ReturnsLargeModelWithHighLimits()
    {
        var result = _sut.GetModelsForTier(UserTier.Enterprise);

        result.EmbeddingModel.Should().Be("text-embedding-3-large");
        result.ChatModel.Should().Be("gpt-4.1");
        result.MaxTokensPerRequest.Should().Be(16384);
        result.MaxDocumentsPerProject.Should().Be(5000);
    }

    [Theory]
    [InlineData(UserTier.Free, 4096)]
    [InlineData(UserTier.Pro, 8192)]
    [InlineData(UserTier.Enterprise, 16384)]
    public void AllTiers_HaveCorrectTokenLimits(UserTier tier, int expectedTokens)
    {
        var result = _sut.GetModelsForTier(tier);
        result.MaxTokensPerRequest.Should().Be(expectedTokens);
    }
}
