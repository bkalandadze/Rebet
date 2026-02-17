using FluentValidation;

namespace Rebet.Application.Commands.Auth;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");

        // Email is optional for MVP but required for current implementation
        // In production, remove this and validate refresh token from storage
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required for MVP implementation")
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

