using BettingPlatform.Application.Commands.Expert;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using ExpertEntity = BettingPlatform.Domain.Entities.Expert;

namespace BettingPlatform.Application.Tests.Expert;

public class ApplyAsExpertCommandHandlerTests
{
    private readonly Mock<IExpertRepository> _expertRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ApplyAsExpertCommandHandler _handler;

    public ApplyAsExpertCommandHandlerTests()
    {
        _expertRepositoryMock = new Mock<IExpertRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _handler = new ApplyAsExpertCommandHandler(
            _expertRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesExpertAndStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ApplyAsExpertCommand
        {
            UserId = userId,
            DisplayName = "Expert Tipster",
            Bio = "Professional football analyst",
            Specialization = "Football"
        };

        var user = new User
        {
            Id = userId,
            Email = "expert@example.com",
            Role = UserRole.User
        };

        ExpertEntity? savedExpert = null;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _expertRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpertEntity?)null);

        _expertRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpertEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ExpertEntity, CancellationToken>((expert, ct) => savedExpert = expert)
            .ReturnsAsync((ExpertEntity expert, CancellationToken ct) => expert);

        _expertRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExpertId.Should().NotBeEmpty();
        result.Message.Should().Contain("submitted successfully");

        savedExpert.Should().NotBeNull();
        savedExpert!.UserId.Should().Be(userId);
        savedExpert.DisplayName.Should().Be(command.DisplayName);
        savedExpert.Bio.Should().Be(command.Bio);
        savedExpert.Specialization.Should().Be(command.Specialization);
        savedExpert.Tier.Should().Be(ExpertTier.Bronze);
        savedExpert.Status.Should().Be(ExpertStatus.PendingApproval);
        savedExpert.CommissionRate.Should().Be(0.1000m);
        savedExpert.IsVerified.Should().BeFalse();
        savedExpert.Statistics.Should().NotBeNull();
        savedExpert.Statistics!.TotalPositions.Should().Be(0);
        savedExpert.Statistics.WinRate.Should().Be(0.00m);

        _expertRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<ExpertEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _expertRepositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserAlreadyHasExpertProfile_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ApplyAsExpertCommand
        {
            UserId = userId,
            DisplayName = "Expert Tipster",
            Bio = "Professional football analyst",
            Specialization = "Football"
        };

        var user = new User
        {
            Id = userId,
            Email = "expert@example.com",
            Role = UserRole.User
        };

        var existingExpert = new ExpertEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Existing Expert",
            Status = ExpertStatus.Active
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _expertRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExpert);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User already has an expert profile");

        _expertRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<ExpertEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var command = new ApplyAsExpertCommand
        {
            UserId = nonExistentUserId,
            DisplayName = "Expert Tipster",
            Bio = "Professional football analyst",
            Specialization = "Football"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentUserId} does not exist");

        _expertRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<ExpertEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

