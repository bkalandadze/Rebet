using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Newsfeed;

public class GetNewsfeedQuery : IRequest<PagedResult<NewsfeedItemDto>>
{
    public string Type { get; set; } = "all"; // "all", "expert", "position", "ticket"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

