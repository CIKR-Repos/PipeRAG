using FluentAssertions;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class FileValidationTests
{
    private readonly DocumentProcessor _processor = new();

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
    [InlineData("text/plain", true)]
    [InlineData("text/markdown", true)]
    [InlineData("text/csv", true)]
    [InlineData("application/json", false)]
    [InlineData("image/png", false)]
    [InlineData("application/zip", false)]
    public void IsSupported_ReturnsExpected(string contentType, bool expected)
    {
        _processor.IsSupported(contentType).Should().Be(expected);
    }

    [Fact]
    public async Task ExtractText_PlainText_ReturnsContent()
    {
        var text = "Hello, this is a test document.";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        var result = await _processor.ExtractTextAsync(stream, "text/plain");
        result.Should().Be(text);
    }

    [Fact]
    public async Task ExtractText_Markdown_ReturnsContent()
    {
        var text = "# Heading\n\nSome **bold** text.";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        var result = await _processor.ExtractTextAsync(stream, "text/markdown");
        result.Should().Be(text);
    }

    [Fact]
    public async Task ExtractText_Csv_ReturnsContent()
    {
        var text = "name,value\nfoo,1\nbar,2";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        var result = await _processor.ExtractTextAsync(stream, "text/csv");
        result.Should().Be(text);
    }

    [Fact]
    public async Task ExtractText_UnsupportedType_Throws()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var act = () => _processor.ExtractTextAsync(stream, "application/json");
        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
