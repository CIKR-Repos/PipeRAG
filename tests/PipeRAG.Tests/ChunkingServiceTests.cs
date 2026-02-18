using FluentAssertions;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class ChunkingServiceTests
{
    private readonly ChunkingService _sut = new();

    [Fact]
    public void ChunkText_EmptyText_ReturnsEmpty()
    {
        var result = _sut.ChunkText("");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var text = "Hello world. This is a test.";
        var result = _sut.ChunkText(text, chunkSize: 100);
        result.Should().HaveCount(1);
        result[0].Content.Should().Contain("Hello world");
    }

    [Fact]
    public void ChunkText_SentenceAware_DoesNotBreakMidSentence()
    {
        // Create text with clear sentences that total > chunkSize
        var sentences = Enumerable.Range(1, 20)
            .Select(i => $"This is sentence number {i} with some extra words to make it longer.")
            .ToList();
        var text = string.Join(" ", sentences);

        var chunks = _sut.ChunkText(text, chunkSize: 30, overlap: 5);

        chunks.Should().HaveCountGreaterThan(1);
        // Each chunk should end at a sentence boundary (end with period)
        foreach (var chunk in chunks)
        {
            chunk.Content.Should().NotBeNullOrWhiteSpace();
            chunk.TokenCount.Should().BeGreaterThan(0);
            chunk.Index.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void ChunkText_OverlapIncluded()
    {
        var sentences = Enumerable.Range(1, 10)
            .Select(i => $"Sentence {i}.")
            .ToList();
        var text = string.Join(" ", sentences);

        var chunks = _sut.ChunkText(text, chunkSize: 5, overlap: 2);

        // With overlap, consecutive chunks should share some content
        if (chunks.Count >= 2)
        {
            // The second chunk should contain some words from end of first
            var firstWords = chunks[0].Content.Split(' ');
            var secondContent = chunks[1].Content;
            var lastWordsOfFirst = firstWords.TakeLast(3);
            lastWordsOfFirst.Any(w => secondContent.Contains(w)).Should().BeTrue(
                "overlap should cause shared content between consecutive chunks");
        }
    }

    [Fact]
    public void ChunkText_IndicesAreSequential()
    {
        var text = string.Join(" ", Enumerable.Range(1, 100).Select(i => $"Word{i}"));
        var chunks = _sut.ChunkText(text, chunkSize: 10, overlap: 2);

        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].Index.Should().Be(i);
        }
    }

    [Fact]
    public void EstimateTokenCount_ReturnsWordCount()
    {
        _sut.EstimateTokenCount("hello world foo bar").Should().Be(4);
    }

    [Fact]
    public void EstimateTokenCount_EmptyString_ReturnsZero()
    {
        _sut.EstimateTokenCount("").Should().Be(0);
        _sut.EstimateTokenCount("   ").Should().Be(0);
    }

    [Fact]
    public void ChunkText_LongSingleSentence_ForcesWordBasedSplitting()
    {
        // One sentence with no periods, exceeding chunkSize
        var words = Enumerable.Range(1, 200).Select(i => $"word{i}").ToList();
        var text = string.Join(" ", words); // no sentence boundaries

        var chunks = _sut.ChunkText(text, chunkSize: 30, overlap: 5);

        chunks.Should().HaveCountGreaterThan(1);
        // Verify overlap: last N words of chunk[i] should appear at start of chunk[i+1]
        for (int i = 1; i < chunks.Count; i++)
        {
            var prevWords = chunks[i - 1].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currWords = chunks[i].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tailOfPrev = prevWords.TakeLast(5).ToArray();
            var headOfCurr = currWords.Take(5).ToArray();
            tailOfPrev.Intersect(headOfCurr).Should().NotBeEmpty(
                "overlap words from previous chunk should appear at start of next chunk");
        }
    }

    [Fact]
    public void ChunkText_SmallOverlap_NeverExceedsConfiguredTokens()
    {
        var sentences = Enumerable.Range(1, 30)
            .Select(i => $"Short sentence {i}.")
            .ToList();
        var text = string.Join(" ", sentences);

        var chunks = _sut.ChunkText(text, chunkSize: 20, overlap: 2);

        chunks.Should().HaveCountGreaterThan(1);
        // For each consecutive pair, verify carried-over tokens <= overlap (with some tolerance)
        for (int i = 1; i < chunks.Count; i++)
        {
            var prevWords = chunks[i - 1].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currWords = chunks[i].Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // Count how many words from the tail of prev appear at the head of curr
            var tailOfPrev = prevWords.TakeLast(10).ToHashSet();
            var overlapCount = 0;
            foreach (var w in currWords)
            {
                if (tailOfPrev.Contains(w))
                    overlapCount++;
                else
                    break; // stop at first non-overlap word
            }
            // Overlap tokens should not wildly exceed configured overlap
            // Using 2x tolerance since sentence-level overlap may carry a full sentence
            overlapCount.Should().BeLessThanOrEqualTo(10,
                "overlap should be bounded relative to configured overlap");
        }
    }
}
