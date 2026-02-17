using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using MediatR;

namespace Rebet.Application.Commands.Auth;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // MVP Implementation: 
        // In production, you should store refresh tokens in database/cache and validate them.
        // For MVP, we require email to identify the user. This should be enhanced with proper token storage.
        
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new UnauthorizedAccessException("Email is required for refresh token validation in MVP. Enhance with token storage for production.");
        }

        // Get user by email
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Note: In production, validate the refresh token against stored tokens:
        // - Create RefreshToken entity with UserId, Token, ExpiresAt, CreatedAt
        // - Store tokens in database or distributed cache (Redis)
        // - Validate token exists, matches user, and hasn't expired
        // - Revoke old token and store new token
        // Example implementation:
        // var tokenData = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        // if (tokenData == null || tokenData.UserId != user.Id || tokenData.ExpiresAt < DateTime.UtcNow)
        //     throw new UnauthorizedAccessException("Invalid or expired refresh token");
        // await _refreshTokenRepository.RevokeTokenAsync(tokenData.Id, cancellationToken);

        // Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user);

        // Note: Store new refresh token in database/cache
        // await _refreshTokenRepository.SaveTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(7), cancellationToken);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600 // 1 hour in seconds
        };
    }
}

