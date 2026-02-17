using Rebet.API.Common;
using Rebet.Application.DTOs;
using Rebet.Application.Queries.Newsfeed;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/newsfeed")]
[ApiVersion("1.0")]
public class NewsfeedController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NewsfeedController> _logger;

    public NewsfeedController(IMediator mediator, ILogger<NewsfeedController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get newsfeed items with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NewsfeedItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNewsfeed(
        [FromQuery] string type = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetNewsfeedQuery
            {
                Type = type,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PagedResult<NewsfeedItemDto>>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query parameters: {Message}", ex.Message);
            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching newsfeed");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching newsfeed"
                }
            });
        }
    }
}

