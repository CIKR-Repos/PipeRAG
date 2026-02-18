using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Api.Controllers;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Infrastructure.Data;
using System.Security.Claims;

namespace PipeRAG.Tests;

public class WidgetControllerTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly WidgetController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public WidgetControllerTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase($"WidgetTest_{Guid.NewGuid()}")
            .Options;
        _db = new PipeRagDbContext(options);

        _db.Users.Add(new User { Id = _userId, Email = "test@test.com", DisplayName = "Test", PasswordHash = "hash" });
        _db.Projects.Add(new Project { Id = _projectId, Name = "Test Project", OwnerId = _userId });
        _db.SaveChanges();

        _controller = new WidgetController(_db);
        SetUser(_userId);
    }

    private void SetUser(Guid userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenNoConfig()
    {
        var result = await _controller.Get(_projectId, CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenProjectNotOwned()
    {
        SetUser(Guid.NewGuid());
        var result = await _controller.Get(_projectId, CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Upsert_CreatesNewConfig()
    {
        var request = new WidgetConfigRequest(PrimaryColor: "#ff0000", Title: "My Widget");
        var result = await _controller.Upsert(_projectId, request, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == _projectId);
        config.Should().NotBeNull();
        config!.PrimaryColor.Should().Be("#ff0000");
        config.Title.Should().Be("My Widget");
    }

    [Fact]
    public async Task Upsert_UpdatesExistingConfig()
    {
        _db.WidgetConfigs.Add(new WidgetConfig { ProjectId = _projectId, PrimaryColor = "#000000" });
        await _db.SaveChangesAsync();

        var request = new WidgetConfigRequest(PrimaryColor: "#ff0000");
        await _controller.Upsert(_projectId, request, CancellationToken.None);

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == _projectId);
        config!.PrimaryColor.Should().Be("#ff0000");
    }

    [Fact]
    public async Task Upsert_OnlyUpdatesProvidedFields()
    {
        _db.WidgetConfigs.Add(new WidgetConfig
        {
            ProjectId = _projectId, PrimaryColor = "#000000", Title = "Original",
            BackgroundColor = "#111111"
        });
        await _db.SaveChangesAsync();

        var request = new WidgetConfigRequest(PrimaryColor: "#ff0000");
        await _controller.Upsert(_projectId, request, CancellationToken.None);

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == _projectId);
        config!.PrimaryColor.Should().Be("#ff0000");
        config.Title.Should().Be("Original");
        config.BackgroundColor.Should().Be("#111111");
    }

    [Fact]
    public async Task Upsert_ReturnsNotFound_ForNonOwnedProject()
    {
        SetUser(Guid.NewGuid());
        var request = new WidgetConfigRequest(PrimaryColor: "#ff0000");
        var result = await _controller.Upsert(_projectId, request, CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_RemovesConfig()
    {
        _db.WidgetConfigs.Add(new WidgetConfig { ProjectId = _projectId });
        await _db.SaveChangesAsync();

        var result = await _controller.Delete(_projectId, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();

        var config = await _db.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectId == _projectId);
        config.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenNoConfig()
    {
        var result = await _controller.Delete(_projectId, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Get_ReturnsConfig_AfterUpsert()
    {
        var request = new WidgetConfigRequest(
            PrimaryColor: "#abcdef",
            Position: "bottom-left",
            Title: "Test Chat");
        await _controller.Upsert(_projectId, request, CancellationToken.None);

        var result = await _controller.Get(_projectId, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
        var response = (result.Result as OkObjectResult)!.Value as WidgetConfigResponse;
        response!.PrimaryColor.Should().Be("#abcdef");
        response.Position.Should().Be("bottom-left");
        response.Title.Should().Be("Test Chat");
    }

    public void Dispose() => _db.Dispose();
}
