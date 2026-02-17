using FluentValidation;

namespace Rebet.Application.Commands.Position;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.SportEventId)
            .NotEmpty().WithMessage("Sport event ID is required");

        RuleFor(x => x.Market)
            .NotEmpty().WithMessage("Market is required")
            .MaximumLength(100).WithMessage("Market must not exceed 100 characters");

        RuleFor(x => x.Selection)
            .NotEmpty().WithMessage("Selection is required")
            .MaximumLength(100).WithMessage("Selection must not exceed 100 characters");

        RuleFor(x => x.Odds)
            .NotEmpty().WithMessage("Odds are required")
            .GreaterThanOrEqualTo(1.01m).WithMessage("Odds must be at least 1.01")
            .LessThanOrEqualTo(1000.00m).WithMessage("Odds must not exceed 1000.00")
            .PrecisionScale(10, 2, false).WithMessage("Odds must have at most 2 decimal places");

        RuleFor(x => x.Analysis)
            .MaximumLength(5000).WithMessage("Analysis must not exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Analysis));

        RuleFor(x => x.CreatorId)
            .NotEmpty().WithMessage("Creator ID is required");
    }
}

