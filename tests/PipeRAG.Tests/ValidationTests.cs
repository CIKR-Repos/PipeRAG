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
    public void RegisterRequest_Invalid_PasswordTooLong()
    {
        var validator = new RegisterRequestValidator();
        var longPassword = "Aa1!" + new string('a', 125); // 129 chars
        var result = validator.Validate(new RegisterRequest("test@test.com", longPassword, "Test"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RegisterRequest_Invalid_EmailTooLong()
    {
        var validator = new RegisterRequestValidator();
        var longEmail = $"user@{new string('a', 250)}.com";
        var result = validator.Validate(new RegisterRequest(longEmail, "Password1!", "Test"));
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
    public void LoginRequest_Invalid_EmailFormat()
    {
        var validator = new LoginRequestValidator();
        var result = validator.Validate(new LoginRequest("not-an-email", "Password1!"));
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
    public void UpdateProfileRequest_Invalid_DisplayNameTooLong()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest(new string('a', 101), "valid@email.com"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateProfileRequest_Valid_NullFields()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.Validate(new UpdateProfileRequest(null, null));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RefreshToken_Empty_Invalid()
    {
        var validator = new RefreshTokenRequestValidator();
        var result = validator.Validate(new RefreshTokenRequest(""));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshTokenRequest_Valid()
    {
        var validator = new RefreshTokenRequestValidator();
        var result = validator.Validate(new RefreshTokenRequest("valid-token"));
        Assert.True(result.IsValid);
    }
}
