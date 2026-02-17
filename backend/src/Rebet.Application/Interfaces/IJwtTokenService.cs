using Rebet.Domain.Entities;

namespace Rebet.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
}

