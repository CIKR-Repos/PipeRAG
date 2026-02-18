namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Extracts text content from uploaded document files.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Extracts text from a file stream based on content type.
    /// </summary>
    Task<string> ExtractTextAsync(Stream fileStream, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Returns whether the given content type is supported.
    /// </summary>
    bool IsSupported(string contentType);
}
