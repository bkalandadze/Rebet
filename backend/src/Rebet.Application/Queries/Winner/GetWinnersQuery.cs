using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Winner;

public class GetWinnersQuery : IRequest<PagedResult<WinnerDto>>
{
    public string? Sport { get; set; }
    public string? Period { get; set; } // "today", "week", "month"
    public decimal? MinOdds { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class WinnerDto
{
    public ExpertWinnerDto Expert { get; set; } = null!;
    public int SubscriberCount { get; set; }
    public int TotalPredictions { get; set; }
    public WinnerPositionDto Position { get; set; } = null!;
    public string Profit { get; set; } = null!;
    public DateTime WonAt { get; set; }
}

public class WinnerPositionDto
{
    public Guid Id { get; set; }
    public string Event { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string Result { get; set; } = null!;
}

public class ExpertWinnerDto
{
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
}

