using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Commands.Expert;

public class ApplyAsExpertCommandHandler : IRequestHandler<ApplyAsExpertCommand, ApplyAsExpertResponse>
{
    private readonly IExpertRepository _expertRepository;
    private readonly IUserRepository _userRepository;

    public ApplyAsExpertCommandHandler(
        IExpertRepository expertRepository,
        IUserRepository userRepository)
    {
        _expertRepository = expertRepository;
        _userRepository = userRepository;
    }

    public async Task<ApplyAsExpertResponse> Handle(ApplyAsExpertCommand request, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} does not exist");
        }

        // Check if user already has an expert profile
        var existingExpert = await _expertRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingExpert != null)
        {
            throw new InvalidOperationException("User already has an expert profile");
        }

        // Create Expert entity
        var expert = new Domain.Entities.Expert
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DisplayName = request.DisplayName,
            Bio = request.Bio,
            Specialization = request.Specialization,
            Tier = ExpertTier.Bronze,
            Status = ExpertStatus.PendingApproval,
            CommissionRate = 0.1000m, // 10%
            IsVerified = false,
            UpvoteCount = 0,
            DownvoteCount = 0,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        // Create ExpertStatistics entity (all zeros initially)
        var statistics = new Domain.Entities.ExpertStatistics
        {
            ExpertId = expert.Id,
            TotalPositions = 0,
            TotalTickets = 0,
            WonPositions = 0,
            LostPositions = 0,
            VoidPositions = 0,
            PendingPositions = 0,
            WinRate = 0.00m,
            ROI = 0.00m,
            AverageOdds = 0.00m,
            CurrentStreak = 0,
            LongestWinStreak = 0,
            Last7DaysWinRate = 0.00m,
            Last30DaysWinRate = 0.00m,
            Last90DaysWinRate = 0.00m,
            TotalProfit = 0.00m,
            TotalCommissionEarned = 0.00m,
            TotalSubscribers = 0,
            ActiveSubscribers = 0,
            LastCalculatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Expert = expert
        };

        expert.Statistics = statistics;

        // Save to database
        await _expertRepository.AddAsync(expert, cancellationToken);
        await _expertRepository.SaveChangesAsync(cancellationToken);

        return new ApplyAsExpertResponse
        {
            ExpertId = expert.Id,
            Message = "Expert application submitted successfully. Your application is pending approval."
        };
    }
}

