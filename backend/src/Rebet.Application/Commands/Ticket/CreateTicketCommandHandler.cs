using Rebet.Application.DTOs;
using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using DomainEntities = Rebet.Domain.Entities;
using MediatR;

namespace Rebet.Application.Commands.Ticket;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ISportEventRepository _sportEventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;

    public CreateTicketCommandHandler(
        ITicketRepository ticketRepository,
        ISportEventRepository sportEventRepository,
        IUserRepository userRepository,
        IMediator mediator)
    {
        _ticketRepository = ticketRepository;
        _sportEventRepository = sportEventRepository;
        _userRepository = userRepository;
        _mediator = mediator;
    }

    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        // Use transaction to ensure all-or-nothing creation
        await _ticketRepository.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Validate expert exists
            var expert = await _userRepository.GetByIdAsync(request.ExpertId, cancellationToken);
            if (expert == null)
            {
                throw new InvalidOperationException($"User with ID {request.ExpertId} does not exist");
            }

            // Validate expert is an expert
            if (expert.Role != UserRole.Expert)
            {
                throw new InvalidOperationException($"User with ID {request.ExpertId} is not an expert");
            }

            // Validate all sport events exist and are scheduled
            var sportEventIds = request.Entries.Select(e => e.SportEventId).Distinct().ToList();
            var sportEvents = new List<SportEvent>();

            foreach (var eventId in sportEventIds)
            {
                var sportEvent = await _sportEventRepository.GetByIdAsync(eventId, cancellationToken);
                if (sportEvent == null)
                {
                    throw new InvalidOperationException($"Sport event with ID {eventId} does not exist");
                }

                if (sportEvent.Status != EventStatus.Scheduled)
                {
                    throw new InvalidOperationException($"Cannot create ticket with event {eventId} that has status {sportEvent.Status}. All events must be scheduled.");
                }

                // Validate event is not more than 7 days in future
                if (sportEvent.StartTimeUtc > DateTime.UtcNow.AddDays(7))
                {
                    throw new InvalidOperationException($"Cannot create ticket with event {eventId} starting more than 7 days in the future");
                }

                sportEvents.Add(sportEvent);
            }

            // Create Ticket entity
            var ticket = new DomainEntities.Ticket
            {
                Id = Guid.NewGuid(),
                ExpertId = request.ExpertId,
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Status = TicketStatus.Draft,
                Stake = request.Stake,
                Visibility = request.Visibility,
                CreatedAt = DateTime.UtcNow
            };

            await _ticketRepository.AddAsync(ticket, cancellationToken);

            // Create TicketEntry entities
            var displayOrder = 0;
            foreach (var entryDto in request.Entries)
            {
                var sportEvent = sportEvents.First(e => e.Id == entryDto.SportEventId);
                
                var entry = new DomainEntities.TicketEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    SportEventId = entryDto.SportEventId,
                    Sport = sportEvent.Sport,
                    League = sportEvent.League,
                    HomeTeam = sportEvent.HomeTeam,
                    AwayTeam = sportEvent.AwayTeam,
                    EventStartTime = sportEvent.StartTimeUtc,
                    Market = entryDto.Market,
                    Selection = entryDto.Selection,
                    Odds = entryDto.Odds,
                    Analysis = entryDto.Analysis,
                    Status = EntryStatus.Pending,
                    DisplayOrder = displayOrder++,
                    CreatedAt = DateTime.UtcNow
                };

                await _ticketRepository.AddEntryAsync(entry, cancellationToken);
                ticket.Entries.Add(entry);
            }

            // Calculate total odds (multiply all entry odds)
            ticket.CalculateTotalOdds();

            // Save all changes
            await _ticketRepository.SaveChangesAsync(cancellationToken);

            // Commit transaction
            await _ticketRepository.CommitTransactionAsync(cancellationToken);

            // Publish TicketCreatedEvent
            var ticketCreatedEvent = new TicketCreatedEvent
            {
                TicketId = ticket.Id,
                ExpertId = ticket.ExpertId,
                Title = ticket.Title,
                Type = ticket.Type.ToString(),
                TotalOdds = ticket.TotalOdds,
                Stake = ticket.Stake,
                EntryCount = ticket.Entries.Count,
                CreatedAt = ticket.CreatedAt
            };

            await _mediator.Publish(ticketCreatedEvent, cancellationToken);

            // Return TicketDto
            return new TicketDto
            {
                Id = ticket.Id,
                ExpertId = ticket.ExpertId,
                Title = ticket.Title,
                Description = ticket.Description,
                Type = ticket.Type.ToString(),
                Status = ticket.Status.ToString(),
                TotalOdds = ticket.TotalOdds,
                Stake = ticket.Stake,
                PotentialReturn = ticket.PotentialReturn,
                Visibility = ticket.Visibility.ToString(),
                Result = ticket.Result?.ToString(),
                FinalOdds = ticket.FinalOdds,
                ViewCount = ticket.ViewCount,
                FollowerCount = ticket.FollowerCount,
                UpvoteCount = ticket.UpvoteCount,
                DownvoteCount = ticket.DownvoteCount,
                CommentCount = ticket.CommentCount,
                CreatedAt = ticket.CreatedAt,
                PublishedAt = ticket.PublishedAt,
                SettledAt = ticket.SettledAt,
                ExpiresAt = ticket.ExpiresAt,
                Entries = ticket.Entries.Select(e => new TicketEntryDto
                {
                    SportEventId = e.SportEventId,
                    Market = e.Market,
                    Selection = e.Selection,
                    Odds = e.Odds,
                    Analysis = e.Analysis
                }).ToList()
            };
        }
        catch
        {
            await _ticketRepository.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

