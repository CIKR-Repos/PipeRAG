using PipeRAG.Api.Validators;
using PipeRAG.Core.DTOs;

namespace PipeRAG.Tests;

public class ValidationTests
{
    [Fact]
    public void RegisterRequest_Valid()
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest("test@test.com", "Password1!", "Test"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Pass1234!", "Name")]
    [InlineData("bad", "Pass1234!", "Name")]
    [InlineData("test@test.com", "short", "Name")]
    [InlineData("test@test.com", "Pass1234!", "")]
    public void RegisterRequest_Invalid(string email, string pass, string name)
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest(email, pass, name));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginRequest_Valid()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest("test@test.com", "password"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LoginRequest_EmptyEmail_Invalid()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest("", "password"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateProfile_Valid()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest("New Name", "new@email.com"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateProfile_InvalidEmail()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest(null, "not-an-email"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshToken_Empty_Invalid()
    {
        var validator = new RefreshTokenRequestValidator();
        var result = validator.Validate(new RefreshTokenRequest(""));
        Assert.False(result.IsValid);
    }
}
