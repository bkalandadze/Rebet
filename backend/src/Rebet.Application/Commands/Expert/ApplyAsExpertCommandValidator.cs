using Rebet.Application.Interfaces;
using FluentValidation;

namespace Rebet.Application.Commands.Expert;

public class ApplyAsExpertCommandValidator : AbstractValidator<ApplyAsExpertCommand>
{
    private readonly IExpertRepository _expertRepository;
    private readonly IUserRepository _userRepository;

    public ApplyAsExpertCommandValidator(
        IExpertRepository expertRepository,
        IUserRepository userRepository)
    {
        _expertRepository = expertRepository;
        _userRepository = userRepository;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters")
            .MustAsync(async (displayName, cancellation) => 
                !await _expertRepository.DisplayNameExistsAsync(displayName, cancellation))
            .WithMessage("Display name is already taken");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.Specialization)
            .MaximumLength(200).WithMessage("Specialization must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Specialization));
    }
}

