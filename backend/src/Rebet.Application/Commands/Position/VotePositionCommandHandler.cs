using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;
using VoteEntity = Rebet.Domain.Entities.Vote;

namespace Rebet.Application.Commands.Position;

public class VotePositionCommandHandler : IRequestHandler<VotePositionCommand, VoteResponse>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IVoteRepository _voteRepository;

    public VotePositionCommandHandler(
        IPositionRepository positionRepository,
        IVoteRepository voteRepository)
    {
        _positionRepository = positionRepository;
        _voteRepository = voteRepository;
    }

    public async Task<VoteResponse> Handle(VotePositionCommand request, CancellationToken cancellationToken)
    {
        // Get position
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position == null || position.IsDeleted)
        {
            throw new KeyNotFoundException($"Position with ID {request.PositionId} not found");
        }

        // Validate vote type
        if (request.VoteType != 1 && request.VoteType != 2)
        {
            throw new ArgumentException("VoteType must be 1 (Upvote) or 2 (Downvote)");
        }

        var newVoteType = request.VoteType == 1 ? VoteType.Upvote : VoteType.Downvote;

        // Check if user already voted
        var existingVote = await _voteRepository.GetVoteAsync(
            request.UserId,
            VoteableType.Position,
            request.PositionId,
            cancellationToken);

        string? voteTypeResult = null;

        if (existingVote != null)
        {
            // User already voted
            if (existingVote.Type == newVoteType)
            {
                // Same vote type - toggle off (remove vote)
                await _voteRepository.RemoveAsync(existingVote, cancellationToken);
                
                // Decrement counts
                if (newVoteType == VoteType.Upvote)
                {
                    position.UpvoteCount = Math.Max(0, position.UpvoteCount - 1);
                }
                else
                {
                    position.DownvoteCount = Math.Max(0, position.DownvoteCount - 1);
                }
                
                position.VoterCount = Math.Max(0, position.VoterCount - 1);
                voteTypeResult = null; // Vote removed
            }
            else
            {
                // Different vote type - change vote
                var oldVoteType = existingVote.Type;
                
                // Decrement old type
                if (oldVoteType == VoteType.Upvote)
                {
                    position.UpvoteCount = Math.Max(0, position.UpvoteCount - 1);
                }
                else
                {
                    position.DownvoteCount = Math.Max(0, position.DownvoteCount - 1);
                }
                
                // Update vote type
                existingVote.Type = newVoteType;
                await _voteRepository.UpdateAsync(existingVote, cancellationToken);
                
                // Increment new type
                if (newVoteType == VoteType.Upvote)
                {
                    position.UpvoteCount++;
                }
                else
                {
                    position.DownvoteCount++;
                }
                
                voteTypeResult = newVoteType == VoteType.Upvote ? "upvote" : "downvote";
            }
        }
        else
        {
            // New vote - create vote record
            var vote = new VoteEntity
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                VoteableType = VoteableType.Position,
                VoteableId = request.PositionId,
                Type = newVoteType,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            await _voteRepository.AddAsync(vote, cancellationToken);
            
            // Increment counts
            if (newVoteType == VoteType.Upvote)
            {
                position.UpvoteCount++;
            }
            else
            {
                position.DownvoteCount++;
            }
            
            position.VoterCount++;
            voteTypeResult = newVoteType == VoteType.Upvote ? "upvote" : "downvote";
        }

        // Recalculate prediction percentage
        position.RecalculatePredictionPercentage();

        // Save changes
        await _positionRepository.SaveChangesAsync(cancellationToken);
        await _voteRepository.SaveChangesAsync(cancellationToken);

        return new VoteResponse
        {
            PositionId = position.Id,
            VoteType = voteTypeResult ?? "",
            UpvoteCount = position.UpvoteCount,
            DownvoteCount = position.DownvoteCount,
            VoterCount = position.VoterCount,
            PredictionPercentage = position.PredictionPercentage
        };
    }
}

