using Rebet.Application.DTOs;
using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;
using VoteEntity = Rebet.Domain.Entities.Vote;

namespace Rebet.Application.Commands.Vote;

public class CastVoteCommandHandler : IRequestHandler<CastVoteCommand, CastVoteResponse>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IExpertRepository _expertRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;

    public CastVoteCommandHandler(
        IVoteRepository voteRepository,
        IPositionRepository positionRepository,
        ITicketRepository ticketRepository,
        IExpertRepository expertRepository,
        IUserRepository userRepository,
        IMediator mediator)
    {
        _voteRepository = voteRepository;
        _positionRepository = positionRepository;
        _ticketRepository = ticketRepository;
        _expertRepository = expertRepository;
        _userRepository = userRepository;
        _mediator = mediator;
    }

    public async Task<CastVoteResponse> Handle(CastVoteCommand request, CancellationToken cancellationToken)
    {
        // Validate VoteableType
        if (!Enum.IsDefined(typeof(VoteableType), request.VoteableType))
        {
            throw new ArgumentException($"Invalid VoteableType: {request.VoteableType}. Must be 1 (Position), 2 (Ticket), or 3 (Expert)");
        }

        // Validate VoteType
        if (!Enum.IsDefined(typeof(VoteType), request.VoteType))
        {
            throw new ArgumentException($"Invalid VoteType: {request.VoteType}. Must be 1 (Upvote) or 2 (Downvote)");
        }

        var voteableType = (VoteableType)request.VoteableType;
        var voteType = (VoteType)request.VoteType;

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        // Use transaction for concurrency handling
        await _voteRepository.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check if vote already exists (with retry for concurrency)
            VoteEntity? existingVote = null;
            int retryCount = 0;
            const int maxRetries = 3;

            while (retryCount < maxRetries)
            {
                try
                {
                    existingVote = await _voteRepository.GetVoteAsync(
                        request.UserId,
                        voteableType,
                        request.VoteableId,
                        cancellationToken);
                    break;
                }
                catch (Exception) when (retryCount < maxRetries - 1)
                {
                    // Retry on any exception (concurrency, database errors, etc.)
                    retryCount++;
                    await Task.Delay(50 * retryCount, cancellationToken); // Exponential backoff
                }
            }

            // Validate the voteable entity exists
            await ValidateVoteableEntityAsync(voteableType, request.VoteableId, cancellationToken);

            string? resultVoteType = null;

            if (existingVote != null)
            {
                // Vote exists - handle toggle or change
                if (existingVote.Type == voteType)
                {
                    // Same type - remove vote (toggle off)
                    await _voteRepository.RemoveAsync(existingVote, cancellationToken);
                    resultVoteType = null; // Vote removed
                }
                else
                {
                    // Different type - update vote type
                    existingVote.Type = voteType;
                    existingVote.IsDeleted = false; // Re-activate if it was soft-deleted
                    resultVoteType = voteType == VoteType.Upvote ? "upvote" : "downvote";
                }
            }
            else
            {
                // New vote - create it
                var newVote = new VoteEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    VoteableType = voteableType,
                    VoteableId = request.VoteableId,
                    Type = voteType,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _voteRepository.AddAsync(newVote, cancellationToken);
                resultVoteType = voteType == VoteType.Upvote ? "upvote" : "downvote";
            }

            // Save changes with concurrency handling for unique constraint violations
            try
            {
                await _voteRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex.Message?.Contains("UNIQUE") == true || 
                                      ex.Message?.Contains("duplicate") == true ||
                                      ex.Message?.Contains("23505") == true ||
                                      ex.InnerException?.Message?.Contains("UNIQUE") == true || 
                                      ex.InnerException?.Message?.Contains("duplicate") == true ||
                                      ex.InnerException?.Message?.Contains("23505") == true) // PostgreSQL unique violation code
            {
                // Unique constraint violation - vote was created by another concurrent request
                // Reload and handle as existing vote
                existingVote = await _voteRepository.GetVoteAsync(
                    request.UserId,
                    voteableType,
                    request.VoteableId,
                    cancellationToken);

                if (existingVote != null)
                {
                    if (existingVote.Type == voteType)
                    {
                        // Same type - remove vote (toggle off)
                        await _voteRepository.RemoveAsync(existingVote, cancellationToken);
                        resultVoteType = null;
                    }
                    else
                    {
                        // Different type - update vote type
                        existingVote.Type = voteType;
                        existingVote.IsDeleted = false;
                        resultVoteType = voteType == VoteType.Upvote ? "upvote" : "downvote";
                    }
                    await _voteRepository.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    throw; // Re-throw if we can't handle it
                }
            }

            // Reload entity to get updated vote counts from database trigger
            // Note: Database trigger updates vote counts, so we need to reload
            var response = await GetUpdatedVoteCountsAsync(
                voteableType,
                request.VoteableId,
                cancellationToken);

            response.VoteType = resultVoteType;

            // Recalculate prediction percentage for positions (after vote counts are updated)
            if (voteableType == VoteableType.Position)
            {
                // Reload position to get updated vote counts from database trigger
                var position = await _positionRepository.GetByIdAsync(request.VoteableId, cancellationToken);
                if (position != null)
                {
                    position.RecalculatePredictionPercentage();
                    await _positionRepository.SaveChangesAsync(cancellationToken);
                    // Update response with recalculated values
                    response.PredictionPercentage = position.PredictionPercentage;
                    response.UpvoteCount = position.UpvoteCount;
                    response.DownvoteCount = position.DownvoteCount;
                    response.VoterCount = position.VoterCount;
                }
            }

            await _voteRepository.CommitTransactionAsync(cancellationToken);

            // Publish VoteCastEvent for cache invalidation
            await _mediator.Publish(new VoteCastEvent
            {
                VoteableId = request.VoteableId,
                VoteableType = voteableType,
                UserId = request.UserId,
                VoteType = resultVoteType != null ? voteType : null, // null if vote was removed
                CastAt = DateTime.UtcNow
            }, cancellationToken);

            return response;
        }
        catch
        {
            await _voteRepository.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task ValidateVoteableEntityAsync(
        VoteableType voteableType,
        Guid voteableId,
        CancellationToken cancellationToken)
    {
        switch (voteableType)
        {
            case VoteableType.Position:
                var position = await _positionRepository.GetByIdAsync(voteableId, cancellationToken);
                if (position == null || position.IsDeleted)
                {
                    throw new KeyNotFoundException($"Position with ID {voteableId} not found");
                }
                break;

            case VoteableType.Ticket:
                var ticket = await _ticketRepository.GetByIdAsync(voteableId, cancellationToken);
                if (ticket == null || ticket.IsDeleted)
                {
                    throw new KeyNotFoundException($"Ticket with ID {voteableId} not found");
                }
                break;

            case VoteableType.Expert:
                var expert = await _expertRepository.GetByIdAsync(voteableId, cancellationToken);
                if (expert == null || expert.IsDeleted)
                {
                    throw new KeyNotFoundException($"Expert with ID {voteableId} not found");
                }
                break;

            default:
                throw new ArgumentException($"Unsupported VoteableType: {voteableType}");
        }
    }

    private async Task<CastVoteResponse> GetUpdatedVoteCountsAsync(
        VoteableType voteableType,
        Guid voteableId,
        CancellationToken cancellationToken)
    {
        var response = new CastVoteResponse
        {
            VoteableId = voteableId,
            VoteableType = voteableType.ToString()
        };

        switch (voteableType)
        {
            case VoteableType.Position:
                var position = await _positionRepository.GetByIdAsync(voteableId, cancellationToken);
                if (position != null)
                {
                    response.UpvoteCount = position.UpvoteCount;
                    response.DownvoteCount = position.DownvoteCount;
                    response.VoterCount = position.VoterCount;
                    response.PredictionPercentage = position.PredictionPercentage;
                }
                break;

            case VoteableType.Ticket:
                var ticket = await _ticketRepository.GetByIdAsync(voteableId, cancellationToken);
                if (ticket != null)
                {
                    response.UpvoteCount = ticket.UpvoteCount;
                    response.DownvoteCount = ticket.DownvoteCount;
                }
                break;

            case VoteableType.Expert:
                var expert = await _expertRepository.GetByIdAsync(voteableId, cancellationToken);
                if (expert != null)
                {
                    response.UpvoteCount = expert.UpvoteCount;
                    response.DownvoteCount = expert.DownvoteCount;
                }
                break;
        }

        return response;
    }
}

