using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PipeRAG.Api.Controllers;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using System.Security.Claims;

namespace PipeRAG.Tests;

public class ChatControllerTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly Mock<IQueryEngineService> _queryEngine = new();
    private readonly Mock<IConversationMemoryService> _memory = new();
    private readonly ChatController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public ChatControllerTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase($"ChatCtrlTest_{Guid.NewGuid()}")
            .Options;
        _db = new PipeRagDbContext(options);

        _db.Users.Add(new User { Id = _userId, Email = "test@test.com", DisplayName = "Test", PasswordHash = "hash" });
        _db.Projects.Add(new Project { Id = _projectId, Name = "Test", OwnerId = _userId });
        _db.SaveChanges();

        _controller = new ChatController(_db, _queryEngine.Object, _memory.Object, Mock.Of<ILogger<ChatController>>());

        // Set up user claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
    }

    [Fact]
    public async Task Chat_ReturnsNotFound_ForInvalidProject()
    {
        var result = await _controller.Chat(Guid.NewGuid(), new ChatRequest("hi"), CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Chat_ReturnsOk_WithValidProject()
    {
        var sessionId = Guid.NewGuid();
        var session = new ChatSession { Id = sessionId, ProjectId = _projectId, UserId = _userId };

        _memory.Setup(m => m.GetOrCreateSessionAsync(null, _projectId, _userId, "hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _queryEngine.Setup(q => q.QueryAsync(
            _projectId, sessionId, "hello", UserTier.Free, "similarity", 5, 0.7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse("Response!", sessionId, [], 42));

        var result = await _controller.Chat(_projectId, new ChatRequest("hello"), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ChatResponse>().Subject;
        response.Message.Should().Be("Response!");
        response.TokensUsed.Should().Be(42);
    }

    [Fact]
    public async Task GetSessions_ReturnsNotFound_ForWrongProject()
    {
        var result = await _controller.GetSessions(Guid.NewGuid(), CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSessions_ReturnsOk_WithSessions()
    {
        _memory.Setup(m => m.GetSessionsAsync(_projectId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ChatSession { Id = Guid.NewGuid(), ProjectId = _projectId, UserId = _userId, Title = "Chat 1", Messages = [new ChatMessage { Id = Guid.NewGuid(), Content = "hi", Role = ChatMessageRole.User }] }
            ]);

        var result = await _controller.GetSessions(_projectId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var sessions = ok.Value.Should().BeAssignableTo<List<ChatSessionResponse>>().Subject;
        sessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteSession_ReturnsNotFound_ForWrongProject()
    {
        var result = await _controller.DeleteSession(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteSession_ReturnsNoContent_OnSuccess()
    {
        _memory.Setup(m => m.DeleteSessionAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.DeleteSession(_projectId, Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();
    }

    public void Dispose() => _db.Dispose();
}
