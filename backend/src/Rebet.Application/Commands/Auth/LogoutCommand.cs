using MediatR;

namespace Rebet.Application.Commands.Auth;

public class LogoutCommand : IRequest<Unit>
{
    public string? RefreshToken { get; set; }
}

