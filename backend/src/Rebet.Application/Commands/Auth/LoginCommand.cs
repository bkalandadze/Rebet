using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Auth;

public class LoginCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

