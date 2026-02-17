using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Auth;

public class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string RefreshToken { get; set; } = null!;
    public string? Email { get; set; } // Optional for MVP - in production, validate refresh token from storage
}

