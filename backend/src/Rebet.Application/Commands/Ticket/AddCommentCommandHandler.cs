using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Commands.Ticket;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommentRepository _commentRepository;

    public AddCommentCommandHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        ICommentRepository commentRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
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

        // Validate parent comment if provided
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _commentRepository.GetByIdAndTypeAsync(
                request.ParentCommentId.Value,
                CommentableType.Ticket,
                request.TicketId,
                cancellationToken);
            
            if (parentComment == null)
            {
                throw new KeyNotFoundException($"Parent comment with ID {request.ParentCommentId.Value} not found");
            }
        }

        // Create comment
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CommentableType = CommentableType.Ticket,
            CommentableId = request.TicketId,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _commentRepository.AddAsync(comment, cancellationToken);

        // Increment comment count on ticket
        ticket.CommentCount++;
        await _ticketRepository.SaveChangesAsync(cancellationToken);

        // Load user profile for display
        var userProfile = await _userRepository.GetUserProfileAsync(request.UserId, cancellationToken);

        return new CommentDto
        {
            Id = comment.Id,
            User = new UserBasicDto
            {
                Id = user.Id,
                DisplayName = userProfile?.DisplayName ?? 
                             (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName)
                                 ? $"{user.FirstName} {user.LastName}".Trim()
                                 : user.Email),
                Avatar = userProfile?.Avatar
            },
            Content = comment.Content,
            ParentCommentId = comment.ParentCommentId,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            Replies = new List<CommentDto>()
        };
    }
}

