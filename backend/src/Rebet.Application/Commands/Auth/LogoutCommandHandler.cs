using MediatR;

namespace Rebet.Application.Commands.Auth;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    public LogoutCommandHandler()
    {
    }

    public Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // For MVP: In a production system, you would:
        // 1. Invalidate the refresh token in storage
        // 2. Add the access token to a blacklist (if using token blacklisting)
        // 3. Clear any session data
        
        // For now, this is a no-op since we don't have token storage
        // The client should discard the tokens
        
        // TODO: Implement token invalidation when refresh token storage is added
        // Example:
        // if (!string.IsNullOrEmpty(request.RefreshToken))
        // {
        //     await _refreshTokenRepository.InvalidateTokenAsync(request.RefreshToken, cancellationToken);
        // }
        
        return Task.FromResult(Unit.Value);
    }
}

