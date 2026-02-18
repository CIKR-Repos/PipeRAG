using FluentAssertions;
using PipeRAG.Infrastructure.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig.Writer;

namespace PipeRAG.Tests;

public class DocumentProcessorTests
{
    private readonly DocumentProcessor _sut = new();

    [Fact]
    public async Task ExtractText_Pdf_ReturnsContent()
    {
        // Create a minimal PDF programmatically using PdfPig
        using var pdfStream = new MemoryStream();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842); // A4-ish
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        page.AddText("Hello from PDF", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        var pdfBytes = builder.Build();
        pdfStream.Write(pdfBytes);
        pdfStream.Position = 0;

        var result = await _sut.ExtractTextAsync(pdfStream, "application/pdf");

        result.Should().Contain("Hello from PDF");
    }

    [Fact]
    public async Task ExtractText_Docx_ReturnsContent()
    {
        // Create a minimal DOCX programmatically using OpenXml
        using var docxStream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(docxStream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(
                        new Run(
                            new Text("Hello from DOCX")))));
            mainPart.Document.Save();
        }
        docxStream.Position = 0;

        var result = await _sut.ExtractTextAsync(docxStream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        result.Should().Contain("Hello from DOCX");
    }

    [Fact]
    public void IsSupported_Pdf_ReturnsTrue()
    {
        _sut.IsSupported("application/pdf").Should().BeTrue();
    }

    [Fact]
    public void IsSupported_Docx_ReturnsTrue()
    {
        _sut.IsSupported("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Should().BeTrue();
    }

    [Fact]
    public void IsSupported_Unknown_ReturnsFalse()
    {
        _sut.IsSupported("application/octet-stream").Should().BeFalse();
    }
}
