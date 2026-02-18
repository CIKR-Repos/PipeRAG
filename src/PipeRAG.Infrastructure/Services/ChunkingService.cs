using PipeRAG.Core.Interfaces;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Recursive, sentence-aware text chunking service.
/// </summary>
public class ChunkingService : IChunkingService
{
    private static readonly char[] SentenceEndings = ['.', '!', '?', '\n'];

    /// <inheritdoc />
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        // Rough approximation: ~0.75 words per token for English text
        // Using whitespace split as a simple proxy (words ≈ tokens * 0.75, so tokens ≈ words / 0.75)
        // Simpler: split on whitespace, count ~1.3 tokens per word is complex.
        // Common heuristic: 1 token ≈ 4 chars, or just count words.
        // We'll use word count as a reasonable approximation.
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <inheritdoc />
    public List<TextChunk> ChunkText(string text, int chunkSize = 512, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var sentences = SplitIntoSentences(text);
        var chunks = new List<TextChunk>();
        var currentTokens = 0;
        var currentSentences = new List<string>();
        var overlapSentences = new List<string>();
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            var sentenceTokens = EstimateTokenCount(sentence);

            // If a single sentence exceeds chunk size, split it by words
            if (sentenceTokens > chunkSize)
            {
                // Flush current buffer first
                if (currentSentences.Count > 0)
                {
                    var chunkContent = string.Join(" ", currentSentences).Trim();
                    if (chunkContent.Length > 0)
                    {
                        chunks.Add(new TextChunk(chunkIndex++, chunkContent, EstimateTokenCount(chunkContent)));
                    }
                    overlapSentences = GetOverlapSentences(currentSentences, overlap);
                    currentSentences.Clear();
                    currentTokens = 0;
                }

                // Split long sentence by words
                var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var wordBuffer = new List<string>();
                var wordTokenCount = 0;

                // Add overlap from previous
                foreach (var os in overlapSentences)
                {
                    wordBuffer.Add(os);
                    wordTokenCount += EstimateTokenCount(os);
                }
                overlapSentences.Clear();

                foreach (var word in words)
                {
                    wordBuffer.Add(word);
                    wordTokenCount++;

                    if (wordTokenCount >= chunkSize)
                    {
                        var content = string.Join(" ", wordBuffer).Trim();
                        chunks.Add(new TextChunk(chunkIndex++, content, EstimateTokenCount(content)));

                        // Keep overlap words
                        var overlapWords = wordBuffer.Skip(Math.Max(0, wordBuffer.Count - overlap)).ToList();
                        wordBuffer.Clear();
                        wordBuffer.AddRange(overlapWords);
                        wordTokenCount = overlapWords.Count;
                    }
                }

                if (wordBuffer.Count > 0)
                {
                    currentSentences.AddRange(wordBuffer.Select(w => w));
                    currentTokens = wordTokenCount;
                }
                continue;
            }

            if (currentTokens + sentenceTokens > chunkSize && currentSentences.Count > 0)
            {
                var chunkContent = string.Join(" ", currentSentences).Trim();
                if (chunkContent.Length > 0)
                {
                    chunks.Add(new TextChunk(chunkIndex++, chunkContent, EstimateTokenCount(chunkContent)));
                }

                overlapSentences = GetOverlapSentences(currentSentences, overlap);
                currentSentences.Clear();
                currentTokens = 0;

                // Add overlap sentences to new chunk
                foreach (var os in overlapSentences)
                {
                    currentSentences.Add(os);
                    currentTokens += EstimateTokenCount(os);
                }
            }

            currentSentences.Add(sentence);
            currentTokens += sentenceTokens;
        }

        // Flush remaining
        if (currentSentences.Count > 0)
        {
            var chunkContent = string.Join(" ", currentSentences).Trim();
            if (chunkContent.Length > 0)
            {
                chunks.Add(new TextChunk(chunkIndex, chunkContent, EstimateTokenCount(chunkContent)));
            }
        }

        return chunks;
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var current = new System.Text.StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            current.Append(text[i]);

            if (SentenceEndings.Contains(text[i]))
            {
                // Consume trailing whitespace
                while (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]) && text[i + 1] != '\n')
                {
                    i++;
                    current.Append(text[i]);
                }

                var sentence = current.ToString().Trim();
                if (sentence.Length > 0)
                    sentences.Add(sentence);
                current.Clear();
            }
        }

        // Remaining text
        var remaining = current.ToString().Trim();
        if (remaining.Length > 0)
            sentences.Add(remaining);

        return sentences;
    }

    private List<string> GetOverlapSentences(List<string> sentences, int overlapTokens)
    {
        var result = new List<string>();
        var tokens = 0;

        for (var i = sentences.Count - 1; i >= 0; i--)
        {
            var sentenceTokens = EstimateTokenCount(sentences[i]);
            if (tokens + sentenceTokens > overlapTokens && result.Count > 0)
                break;
            result.Insert(0, sentences[i]);
            tokens += sentenceTokens;
        }

        return result;
    }
}
