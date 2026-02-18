using FluentValidation;
using PipeRAG.Core.DTOs;

namespace PipeRAG.Api.Validators;

public class WidgetConfigRequestValidator : AbstractValidator<WidgetConfigRequest>
{
    private static readonly string[] ValidPositions = ["bottom-right", "bottom-left"];
    private static readonly System.Text.RegularExpressions.Regex HexColorRegex = new(@"^#[0-9a-fA-F]{6}$");

    public WidgetConfigRequestValidator()
    {
        RuleFor(x => x.PrimaryColor)
            .Must(c => c is null || HexColorRegex.IsMatch(c))
            .WithMessage("PrimaryColor must be a valid hex color (e.g. #6366f1).");

        RuleFor(x => x.BackgroundColor)
            .Must(c => c is null || HexColorRegex.IsMatch(c))
            .WithMessage("BackgroundColor must be a valid hex color.");

        RuleFor(x => x.TextColor)
            .Must(c => c is null || HexColorRegex.IsMatch(c))
            .WithMessage("TextColor must be a valid hex color.");

        RuleFor(x => x.Position)
            .Must(p => p is null || ValidPositions.Contains(p))
            .WithMessage("Position must be 'bottom-right' or 'bottom-left'.");

        RuleFor(x => x.Title)
            .MaximumLength(100).When(x => x.Title is not null);

        RuleFor(x => x.Subtitle)
            .MaximumLength(200).When(x => x.Subtitle is not null);

        RuleFor(x => x.PlaceholderText)
            .MaximumLength(100).When(x => x.PlaceholderText is not null);

        RuleFor(x => x.AvatarUrl)
            .Must(u => u is null || Uri.TryCreate(u, UriKind.Absolute, out _))
            .WithMessage("AvatarUrl must be a valid URL.");

        RuleFor(x => x.AllowedOrigins)
            .MaximumLength(1000).When(x => x.AllowedOrigins is not null);
    }
}
