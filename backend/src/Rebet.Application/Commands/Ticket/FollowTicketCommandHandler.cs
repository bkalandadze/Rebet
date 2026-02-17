using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using MediatR;

namespace Rebet.Application.Commands.Ticket;

public class FollowTicketCommandHandler : IRequestHandler<FollowTicketCommand, FollowTicketResponse>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITicketFollowRepository _ticketFollowRepository;

    public FollowTicketCommandHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        ITicketFollowRepository ticketFollowRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _ticketFollowRepository = ticketFollowRepository;
    }

    public async Task<FollowTicketResponse> Handle(FollowTicketCommand request, CancellationToken cancellationToken)
    {
        // Validate ticket exists
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket == null || ticket.IsDeleted)
        {
            throw new KeyNotFoundException($"Ticket with ID {request.TicketId} not found");
        }

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        // Check if user is already following this ticket
        var existingFollow = await _ticketFollowRepository.GetByUserAndTicketAsync(
            request.UserId,
            request.TicketId,
            cancellationToken);

        bool isFollowing;

        if (existingFollow != null)
        {
            // Unfollow - soft delete the follow record
            existingFollow.IsDeleted = true;
            await _ticketFollowRepository.UpdateAsync(existingFollow, cancellationToken);
            ticket.FollowerCount = Math.Max(0, ticket.FollowerCount - 1);
            isFollowing = false;
        }
        else
        {
            // Follow - create new follow record
            var ticketFollow = new TicketFollow
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                TicketId = request.TicketId,
                FollowedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            
            await _ticketFollowRepository.AddAsync(ticketFollow, cancellationToken);
            ticket.FollowerCount++;
            isFollowing = true;
        }

        await _ticketRepository.SaveChangesAsync(cancellationToken);
        await _ticketFollowRepository.SaveChangesAsync(cancellationToken);

        return new FollowTicketResponse
        {
            TicketId = request.TicketId,
            IsFollowing = isFollowing,
            FollowerCount = ticket.FollowerCount
        };
    }
}

