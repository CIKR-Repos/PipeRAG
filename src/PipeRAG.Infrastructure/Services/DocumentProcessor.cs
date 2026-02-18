using System.Text;
using DocumentFormat.OpenXml.Packaging;
using PipeRAG.Core.Interfaces;
using UglyToad.PdfPig;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// Extracts text from PDF, DOCX, TXT, MD, and CSV files.
/// </summary>
public class DocumentProcessor : IDocumentProcessor
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "text/markdown",
        "text/csv"
    };

    /// <inheritdoc />
    public bool IsSupported(string contentType) => SupportedTypes.Contains(contentType);

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(Stream fileStream, string contentType, CancellationToken ct = default)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => ExtractFromPdf(fileStream),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractFromDocx(fileStream),
            "text/plain" or "text/markdown" or "text/csv" => await ExtractFromTextAsync(fileStream, ct),
            _ => throw new NotSupportedException($"Content type '{contentType}' is not supported.")
        };
    }

    private static string ExtractFromPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractFromDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document.Body;
        return body?.InnerText ?? string.Empty;
    }

    private static async Task<string> ExtractFromTextAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }
}
