using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Api.Controllers;
using PipeRAG.Core.Entities;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Tests;

public class PublicWidgetControllerTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly PublicWidgetController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly string _apiKeyPrefix = "pk_test1";

    public PublicWidgetControllerTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase($"PublicWidgetTest_{Guid.NewGuid()}")
            .Options;
        _db = new PipeRagDbContext(options);

        _db.Users.Add(new User { Id = _userId, Email = "test@test.com", DisplayName = "Test", PasswordHash = "hash" });
        _db.Projects.Add(new Project { Id = _projectId, Name = "Test", OwnerId = _userId });
        _db.ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid(), UserId = _userId, Name = "Widget Key",
            KeyPrefix = _apiKeyPrefix, KeyHash = "hash123", IsActive = true
        });
        _db.WidgetConfigs.Add(new WidgetConfig
        {
            ProjectId = _projectId, PrimaryColor = "#6366f1", IsActive = true,
            AllowedOrigins = "*"
        });
        _db.SaveChanges();

        _controller = new PublicWidgetController(_db);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetConfig_ReturnsBadRequest_WithoutApiKey()
    {
        var result = await _controller.GetConfig(_projectId, "", CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetConfig_ReturnsNotFound_InvalidProject()
    {
        var result = await _controller.GetConfig(Guid.NewGuid(), _apiKeyPrefix + "suffix", CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetConfig_ReturnsUnauthorized_InvalidApiKey()
    {
        var result = await _controller.GetConfig(_projectId, "bad_key_suffix", CancellationToken.None);
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetConfig_ReturnsConfig_WithValidKey()
    {
        var result = await _controller.GetConfig(_projectId, _apiKeyPrefix + "suffix", CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
        var config = (result as OkObjectResult)!.Value as WidgetPublicConfigResponse;
        config!.PrimaryColor.Should().Be("#6366f1");
    }

    [Fact]
    public async Task GetConfig_ReturnsNotFound_WhenDisabled()
    {
        var widget = await _db.WidgetConfigs.FirstAsync(w => w.ProjectId == _projectId);
        widget.IsActive = false;
        await _db.SaveChangesAsync();

        var result = await _controller.GetConfig(_projectId, _apiKeyPrefix + "suffix", CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Chat_ReturnsBadRequest_EmptyMessage()
    {
        var result = await _controller.Chat(_projectId, _apiKeyPrefix + "suffix",
            new WidgetChatRequest(""), CancellationToken.None);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Chat_ReturnsOk_WithValidRequest()
    {
        var result = await _controller.Chat(_projectId, _apiKeyPrefix + "suffix",
            new WidgetChatRequest("Hello"), CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Chat_ReturnsUnauthorized_InvalidKey()
    {
        var result = await _controller.Chat(_projectId, "bad_key_suffix",
            new WidgetChatRequest("Hello"), CancellationToken.None);
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetEmbedScript_ReturnsJavaScript()
    {
        _controller.ControllerContext.HttpContext.Request.Scheme = "https";
        _controller.ControllerContext.HttpContext.Request.Host = new HostString("example.com");

        var result = _controller.GetEmbedScript();
        result.Should().BeOfType<ContentResult>();
        var content = result as ContentResult;
        content!.ContentType.Should().Be("application/javascript");
        content.Content.Should().Contain("piperag_widget_loaded");
        content.Content.Should().Contain("https://example.com");
    }

    public void Dispose() => _db.Dispose();
}
