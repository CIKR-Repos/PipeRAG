using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Infrastructure.Data;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class ConversationMemoryTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly ConversationMemoryService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public ConversationMemoryTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase($"ConvMemTest_{Guid.NewGuid()}")
            .Options;
        _db = new PipeRagDbContext(options);
        _service = new ConversationMemoryService(_db, Mock.Of<ILogger<ConversationMemoryService>>());

        // Seed a project and user
        _db.Users.Add(new User { Id = _userId, Email = "test@test.com", DisplayName = "Test", PasswordHash = "hash" });
        _db.Projects.Add(new Project { Id = _projectId, Name = "Test Project", OwnerId = _userId });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetOrCreateSession_CreatesNewSession_WhenNoSessionId()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "Hello world");

        session.Should().NotBeNull();
        session.ProjectId.Should().Be(_projectId);
        session.UserId.Should().Be(_userId);
        session.Title.Should().Be("Hello world");
    }

    [Fact]
    public async Task GetOrCreateSession_ReturnsExisting_WhenSessionIdProvided()
    {
        var session1 = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "First");
        var session2 = await _service.GetOrCreateSessionAsync(session1.Id, _projectId, _userId);

        session2.Id.Should().Be(session1.Id);
    }

    [Fact]
    public async Task AddMessage_StoresMessage_AndUpdatesLastMessageAt()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "test");
        var msg = await _service.AddMessageAsync(session.Id, ChatMessageRole.User, "Hello!", 10);

        msg.Should().NotBeNull();
        msg.Content.Should().Be("Hello!");
        msg.Role.Should().Be(ChatMessageRole.User);
        msg.TokenCount.Should().Be(10);

        var updated = await _db.ChatSessions.FindAsync(session.Id);
        updated!.LastMessageAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConversationWindow_ReturnsAllMessages_WhenUnderLimit()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "test");
        await _service.AddMessageAsync(session.Id, ChatMessageRole.User, "Q1");
        await _service.AddMessageAsync(session.Id, ChatMessageRole.Assistant, "A1");
        await _service.AddMessageAsync(session.Id, ChatMessageRole.User, "Q2");

        var window = await _service.GetConversationWindowAsync(session.Id, windowSize: 10);

        window.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetConversationWindow_SlidingWindow_SummarizesOlderMessages()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "test");

        // Add 6 messages
        for (int i = 0; i < 6; i++)
        {
            var role = i % 2 == 0 ? ChatMessageRole.User : ChatMessageRole.Assistant;
            await _service.AddMessageAsync(session.Id, role, $"Message {i}");
        }

        // Window of 3 should include summary + 3 recent
        var window = await _service.GetConversationWindowAsync(session.Id, windowSize: 3);

        window.Should().HaveCount(4); // 1 summary + 3 recent
        window[0].Role.Should().Be(ChatMessageRole.System);
        window[0].Content.Should().Contain("Summary");
    }

    [Fact]
    public async Task GetAllMessages_ReturnsAllMessages()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "test");
        await _service.AddMessageAsync(session.Id, ChatMessageRole.User, "Q1");
        await _service.AddMessageAsync(session.Id, ChatMessageRole.Assistant, "A1");

        var all = await _service.GetAllMessagesAsync(session.Id);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteSession_RemovesSession_ReturnsTrue()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "test");
        await _service.AddMessageAsync(session.Id, ChatMessageRole.User, "Q1");

        var result = await _service.DeleteSessionAsync(session.Id, _userId);

        result.Should().BeTrue();
        var exists = await _db.ChatSessions.AnyAsync(s => s.Id == session.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSession_ReturnsFalse_ForWrongUser()
    {
        var session = await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "test");
        var result = await _service.DeleteSessionAsync(session.Id, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetSessions_ReturnsSessionsForUser()
    {
        await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "Session 1");
        await _service.GetOrCreateSessionAsync(null, _projectId, _userId, "Session 2");

        var sessions = await _service.GetSessionsAsync(_projectId, _userId);

        sessions.Should().HaveCount(2);
    }

    public void Dispose() => _db.Dispose();
}
