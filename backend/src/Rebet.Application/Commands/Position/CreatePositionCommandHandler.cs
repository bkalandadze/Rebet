using Rebet.Application.DTOs;
using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Enums;
using DomainEntities = Rebet.Domain.Entities;
using MediatR;

namespace Rebet.Application.Commands.Position;

public class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, PositionDto>
{
    private readonly ISportEventRepository _sportEventRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;

    public CreatePositionCommandHandler(
        ISportEventRepository sportEventRepository,
        IPositionRepository positionRepository,
        IUserRepository userRepository,
        IMediator mediator)
    {
        _sportEventRepository = sportEventRepository;
        _positionRepository = positionRepository;
        _userRepository = userRepository;
        _mediator = mediator;
    }

    public async Task<PositionDto> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        // Validate sport event exists
        var sportEvent = await _sportEventRepository.GetByIdAsync(request.SportEventId, cancellationToken);
        if (sportEvent == null)
        {
            throw new InvalidOperationException($"Sport event with ID {request.SportEventId} does not exist");
        }

        // Validate sport event is scheduled
        if (sportEvent.Status != EventStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot create position for event with status {sportEvent.Status}. Event must be scheduled.");
        }

        // Validate event starts at least 1 hour in the future
        if (sportEvent.StartTimeUtc <= DateTime.UtcNow.AddHours(1))
        {
            throw new InvalidOperationException("Cannot create position for event starting within 1 hour");
        }

        // Get creator to determine creator type
        var creator = await _userRepository.GetByIdAsync(request.CreatorId, cancellationToken);
        if (creator == null)
        {
            throw new InvalidOperationException($"User with ID {request.CreatorId} does not exist");
        }

        // Create position entity
        var position = new DomainEntities.Position
        {
            Id = Guid.NewGuid(),
            CreatorId = request.CreatorId,
            CreatorType = creator.Role,
            SportEventId = request.SportEventId,
            Market = request.Market,
            Selection = request.Selection,
            Odds = request.Odds,
            Analysis = request.Analysis,
            Status = PositionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Add position to repository
        await _positionRepository.AddAsync(position, cancellationToken);
        await _positionRepository.SaveChangesAsync(cancellationToken);

        // Publish PositionCreatedEvent
        var positionCreatedEvent = new PositionCreatedEvent
        {
            PositionId = position.Id,
            CreatorId = position.CreatorId,
            CreatorType = position.CreatorType.ToString(),
            SportEventId = position.SportEventId,
            Market = position.Market,
            Selection = position.Selection,
            Odds = position.Odds,
            CreatedAt = position.CreatedAt
        };

        await _mediator.Publish(positionCreatedEvent, cancellationToken);

        // Return PositionDto
        return new PositionDto
        {
            Id = position.Id,
            CreatorId = position.CreatorId,
            CreatorType = position.CreatorType.ToString(),
            SportEventId = position.SportEventId,
            Market = position.Market,
            Selection = position.Selection,
            Odds = position.Odds,
            Analysis = position.Analysis,
            Status = position.Status.ToString(),
            Result = position.Result?.ToString(),
            ViewCount = position.ViewCount,
            UpvoteCount = position.UpvoteCount,
            DownvoteCount = position.DownvoteCount,
            VoterCount = position.VoterCount,
            PredictionPercentage = position.PredictionPercentage,
            CreatedAt = position.CreatedAt,
            SettledAt = position.SettledAt
        };
    }
}

