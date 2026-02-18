using PipeRAG.Core.Entities;

namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Service for splitting text into chunks for RAG processing.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Splits text into chunks with configurable size and overlap.
    /// </summary>
    List<TextChunk> ChunkText(string text, int chunkSize = 512, int overlap = 50);

    /// <summary>
    /// Estimates token count for a piece of text (whitespace-based approximation).
    /// </summary>
    int EstimateTokenCount(string text);
}

/// <summary>
/// Represents a chunk of text produced by the chunking service.
/// </summary>
public record TextChunk(int Index, string Content, int TokenCount);
