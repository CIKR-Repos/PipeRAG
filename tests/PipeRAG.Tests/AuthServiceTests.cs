using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PipeRAG.Core.DTOs;
using PipeRAG.Infrastructure.Data;
using PipeRAG.Infrastructure.Services;

namespace PipeRAG.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly PipeRagDbContext _db;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<PipeRagDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PipeRagDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "PipeRAG-Test-Secret-Key-Min32Characters-Long!!",
                ["Jwt:Issuer"] = "PipeRAG-Test",
                ["Jwt:Audience"] = "PipeRAG-Test",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();

        _sut = new AuthService(_db, config);
    }

    [Fact]
    public async Task Register_CreatesUser_ReturnsTokens()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("test@test.com", "Password123!", "Test User"));

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("test@test.com", result.User.Email);
        Assert.Equal("Test User", result.User.DisplayName);
        Assert.Equal(Core.Enums.UserTier.Free, result.User.Tier);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        await _sut.RegisterAsync(new RegisterRequest("dup@test.com", "Password123!", "User1"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(new RegisterRequest("dup@test.com", "Password456!", "User2")));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        await _sut.RegisterAsync(new RegisterRequest("login@test.com", "Password123!", "Login User"));
        var result = await _sut.LoginAsync(new LoginRequest("login@test.com", "Password123!"));

        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("login@test.com", result.User.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        await _sut.RegisterAsync(new RegisterRequest("wrong@test.com", "Password123!", "User"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(new LoginRequest("wrong@test.com", "WrongPassword!")));
    }

    [Fact]
    public async Task Login_DeactivatedUser_Throws()
    {
        await _sut.RegisterAsync(new RegisterRequest("inactive@test.com", "Password123!", "Inactive User"));
        var user = await _db.Users.SingleAsync(u => u.Email == "inactive@test.com");
        user.IsActive = false;
        await _db.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(new LoginRequest("inactive@test.com", "Password123!")));
        Assert.Equal("Account is deactivated.", exception.Message);
    }

    [Fact]
    public async Task Login_NonexistentUser_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.LoginAsync(new LoginRequest("nobody@test.com", "Password123!")));
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var initial = await _sut.RegisterAsync(new RegisterRequest("refresh@test.com", "Password123!", "User"));
        var refreshed = await _sut.RefreshTokenAsync(initial.RefreshToken);

        Assert.NotEmpty(refreshed.AccessToken);
        Assert.NotEqual(initial.AccessToken, refreshed.AccessToken);
        Assert.NotEqual(initial.RefreshToken, refreshed.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RefreshTokenAsync("invalid-token"));
    }

    [Fact]
    public async Task Refresh_UsedToken_Throws()
    {
        var initial = await _sut.RegisterAsync(new RegisterRequest("used@test.com", "Password123!", "User"));
        await _sut.RefreshTokenAsync(initial.RefreshToken);
        // Second use should fail (token revoked)
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RefreshTokenAsync(initial.RefreshToken));
    }

    public void Dispose() => _db.Dispose();
}
