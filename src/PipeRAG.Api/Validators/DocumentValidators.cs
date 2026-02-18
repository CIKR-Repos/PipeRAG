using FluentValidation;
using PipeRAG.Core.DTOs;

namespace PipeRAG.Api.Validators;

/// <summary>
/// Validates chunk preview request parameters.
/// </summary>
public class ChunkPreviewRequestValidator : AbstractValidator<ChunkPreviewRequest>
{
    public ChunkPreviewRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}
