using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PipeRAG.Api.Controllers;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Infrastructure.Data;
using PipeRAG.Core.DTOs;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class DocumentControllerTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly DocumentsController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly string _tempDir;

    public DocumentControllerTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PipeRagDbContext(options);

        _tempDir = Path.Combine(Path.GetTempPath(), "piperag-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        var storage = new LocalFileStorageService(_tempDir);
        var processor = new DocumentProcessor();
        var chunking = new ChunkingService();
        var logger = LoggerFactory.Create(b => { }).CreateLogger<DocumentsController>();

        _controller = new DocumentsController(_db, storage, processor, chunking, logger);

        // Set up auth
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // Seed project + user
        _db.Users.Add(new User { Id = _userId, Email = "test@test.com", DisplayName = "Test", PasswordHash = "x" });
        _db.Projects.Add(new Project { Id = _projectId, Name = "Test Project", OwnerId = _userId });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Upload_ValidTextFile_ReturnsSuccess()
    {
        var content = "Hello world. This is test content. Another sentence here.";
        var file = CreateFormFile("test.txt", content);

        var result = await _controller.Upload(_projectId, [file], CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Core.DTOs.DocumentUploadResponse>().Subject;
        response.TotalFiles.Should().Be(1);
        response.SuccessCount.Should().Be(1);
        response.FailedCount.Should().Be(0);
        response.Documents.Should().HaveCount(1);
        response.Documents[0].Status.Should().Be("Chunked");
    }

    [Fact]
    public async Task Upload_UnsupportedFileType_SkipsFile()
    {
        var file = CreateFormFile("test.exe", "bad content");
        var result = await _controller.Upload(_projectId, [file], CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Core.DTOs.DocumentUploadResponse>().Subject;
        response.FailedCount.Should().Be(1);
        response.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsDocuments()
    {
        // Upload first
        var file = CreateFormFile("test.txt", "content");
        await _controller.Upload(_projectId, [file], CancellationToken.None);

        var result = await _controller.List(_projectId, CancellationToken.None);
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var docs = ok.Value.Should().BeAssignableTo<List<Core.DTOs.DocumentResponse>>().Subject;
        docs.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetChunks_ReturnsPaginatedChunks()
    {
        var file = CreateFormFile("test.txt", "First sentence. Second sentence. Third sentence.");
        await _controller.Upload(_projectId, [file], CancellationToken.None);

        var doc = await _db.Documents.FirstAsync();
        var result = await _controller.GetChunks(_projectId, doc.Id, new ChunkPreviewRequest(1, 10), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var preview = ok.Value.Should().BeOfType<Core.DTOs.ChunkPreviewResponse>().Subject;
        preview.Chunks.Should().NotBeEmpty();
        preview.Page.Should().Be(1);
    }

    [Fact]
    public async Task Delete_RemovesDocumentAndChunks()
    {
        var file = CreateFormFile("test.txt", "Some content here.");
        await _controller.Upload(_projectId, [file], CancellationToken.None);

        var doc = await _db.Documents.FirstAsync();
        var deleteResult = await _controller.Delete(_projectId, doc.Id, CancellationToken.None);
        deleteResult.Should().BeOfType<NoContentResult>();

        (await _db.Documents.CountAsync()).Should().Be(0);
        (await _db.DocumentChunks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Get_NonExistent_ReturnsNotFound()
    {
        var result = await _controller.Get(_projectId, Guid.NewGuid(), CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Upload_FileOverFreeTierLimit_Fails()
    {
        var bigStream = new MemoryStream(new byte[100]); // small backing array
        var file = new FormFile(bigStream, 0, 51L * 1024 * 1024, "files", "big.txt");

        var result = await _controller.Upload(_projectId, [file], CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Core.DTOs.DocumentUploadResponse>().Subject;
        response.TotalFiles.Should().Be(1);
        response.SuccessCount.Should().Be(0);
        response.FailedCount.Should().Be(1);
        (await _db.Documents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Upload_FileUnderProTierLimit_Succeeds()
    {
        // Upgrade user to Pro
        var user = await _db.Users.FindAsync(_userId);
        user!.Tier = UserTier.Pro;
        await _db.SaveChangesAsync();

        // 51MB file - over Free limit but under Pro limit
        var bigStream = new MemoryStream(new byte[100]);
        var file = new FormFile(bigStream, 0, 51L * 1024 * 1024, "files", "big.txt");

        var result = await _controller.Upload(_projectId, [file], CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Core.DTOs.DocumentUploadResponse>().Subject;
        response.SuccessCount.Should().Be(1);
        response.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task Upload_MultiFile_OneValidOneTooLarge_PartialSuccess()
    {
        var validFile = CreateFormFile("small.txt", "Hello world content here.");
        var bigStream = new MemoryStream(new byte[100]);
        var bigFile = new FormFile(bigStream, 0, 51L * 1024 * 1024, "files", "big.txt");

        var result = await _controller.Upload(_projectId, [validFile, bigFile], CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<Core.DTOs.DocumentUploadResponse>().Subject;
        response.TotalFiles.Should().Be(2);
        response.SuccessCount.Should().Be(1);
        response.FailedCount.Should().Be(1);
        (await _db.Documents.CountAsync()).Should().Be(1);
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "files", fileName);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
