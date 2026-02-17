using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using MediatR;

namespace Rebet.Application.Queries.Newsfeed;

public class GetNewsfeedQueryHandler : IRequestHandler<GetNewsfeedQuery, PagedResult<NewsfeedItemDto>>
{
    private readonly INewsfeedRepository _newsfeedRepository;

    public GetNewsfeedQueryHandler(INewsfeedRepository newsfeedRepository)
    {
        _newsfeedRepository = newsfeedRepository;
    }

    public async Task<PagedResult<NewsfeedItemDto>> Handle(GetNewsfeedQuery request, CancellationToken cancellationToken)
    {
        return await _newsfeedRepository.GetNewsfeedAsync(
            request.Type,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}

