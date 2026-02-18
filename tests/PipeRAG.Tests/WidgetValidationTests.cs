using FluentAssertions;
using FluentValidation.TestHelper;
using PipeRAG.Api.Validators;
using PipeRAG.Core.DTOs;

namespace PipeRAG.Tests;

public class WidgetValidationTests
{
    private readonly WidgetConfigRequestValidator _validator = new();

    [Theory]
    [InlineData("#6366f1")]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData(null)]
    public void ValidColors_ShouldPass(string? color)
    {
        var request = new WidgetConfigRequest(PrimaryColor: color);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.PrimaryColor);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#fff")]
    [InlineData("#gggggg")]
    [InlineData("6366f1")]
    public void InvalidColors_ShouldFail(string color)
    {
        var request = new WidgetConfigRequest(PrimaryColor: color);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PrimaryColor);
    }

    [Theory]
    [InlineData("bottom-right")]
    [InlineData("bottom-left")]
    [InlineData(null)]
    public void ValidPositions_ShouldPass(string? position)
    {
        var request = new WidgetConfigRequest(Position: position);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Position);
    }

    [Theory]
    [InlineData("top-right")]
    [InlineData("center")]
    [InlineData("left")]
    public void InvalidPositions_ShouldFail(string position)
    {
        var request = new WidgetConfigRequest(Position: position);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Position);
    }

    [Fact]
    public void TitleTooLong_ShouldFail()
    {
        var request = new WidgetConfigRequest(Title: new string('a', 101));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void ValidAvatarUrl_ShouldPass()
    {
        var request = new WidgetConfigRequest(AvatarUrl: "https://example.com/avatar.png");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Fact]
    public void InvalidAvatarUrl_ShouldFail()
    {
        var request = new WidgetConfigRequest(AvatarUrl: "not-a-url");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AvatarUrl);
    }
}
