using Rebet.Application.DTOs;
using Rebet.Domain.Enums;
using FluentValidation;

namespace Rebet.Application.Commands.Ticket;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.ExpertId)
            .NotEmpty().WithMessage("Expert ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid ticket type");

        RuleFor(x => x.Stake)
            .GreaterThanOrEqualTo(0.01m).WithMessage("Stake must be at least 0.01")
            .LessThanOrEqualTo(10000.00m).WithMessage("Stake must not exceed 10000.00")
            .PrecisionScale(10, 2, false).WithMessage("Stake must have at most 2 decimal places");

        RuleFor(x => x.Visibility)
            .IsInEnum().WithMessage("Invalid visibility type");

        RuleFor(x => x.Entries)
            .NotEmpty().WithMessage("At least one entry is required")
            .Must(entries => entries != null && entries.Count > 0)
            .WithMessage("At least one entry is required");

        // Entry count validation based on ticket type
        RuleFor(x => x.Entries)
            .Must((command, entries) => 
                command.Type == TicketType.Single ? entries.Count == 1 :
                command.Type == TicketType.Multi ? entries.Count >= 2 && entries.Count <= 20 :
                command.Type == TicketType.System ? entries.Count >= 3 && entries.Count <= 15 :
                true)
            .WithMessage("Entry count does not match ticket type requirements");

        // Validate each entry
        RuleForEach(x => x.Entries)
            .SetValidator(new TicketEntryDtoValidator());

        // No duplicate events
        RuleFor(x => x.Entries)
            .Must(entries => entries.Select(e => e.SportEventId).Distinct().Count() == entries.Count)
            .WithMessage("Cannot have duplicate events in the same ticket");
    }
}

public class TicketEntryDtoValidator : AbstractValidator<TicketEntryDto>
{
    public TicketEntryDtoValidator()
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
            .GreaterThanOrEqualTo(1.01m).WithMessage("Odds must be at least 1.01")
            .LessThanOrEqualTo(1000.00m).WithMessage("Odds must not exceed 1000.00")
            .PrecisionScale(10, 2, false).WithMessage("Odds must have at most 2 decimal places");

        RuleFor(x => x.Analysis)
            .MaximumLength(5000).WithMessage("Analysis must not exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Analysis));
    }
}

