using BettingPlatform.Application.Commands.Auth;
using BettingPlatform.Application.DTOs;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BettingPlatform.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new RefreshTokenCommandHandler(
            _userRepositoryMock.Object,
            _jwtTokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidEmail_ReturnsNewTokens()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            Email = "user@example.com",
            RefreshToken = "old_refresh_token"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(s => s.GenerateAccessToken(user))
            .Returns(newAccessToken);

        _jwtTokenServiceMock
            .Setup(s => s.GenerateRefreshToken(user))
            .Returns(newRefreshToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRefreshToken);
        result.ExpiresIn.Should().BeGreaterThan(0);

        _userRepositoryMock.Verify(
            r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyEmail_ThrowsException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            Email = "",
            RefreshToken = "some_token"
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Email is required*");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsException()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            Email = "nonexistent@example.com",
            RefreshToken = "some_token"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token");
    }
}

