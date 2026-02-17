using Rebet.Application.DTOs;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Commands.Ticket;

public class CreateTicketCommand : IRequest<TicketDto>
{
    public Guid ExpertId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TicketType Type { get; set; }
    public decimal Stake { get; set; }
    public TicketVisibility Visibility { get; set; } = TicketVisibility.Public;
    public List<TicketEntryDto> Entries { get; set; } = new();
}

