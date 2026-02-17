using Rebet.API.Common;
using Rebet.Application.DTOs;
using Rebet.Application.Queries.Event;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/events")]
[ApiVersion("1.0")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IMediator mediator, ILogger<EventsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all events with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EventListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllEvents(
        [FromQuery] string? sport = null,
        [FromQuery] string? league = null,
        [FromQuery] string? date = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? hasExpertPredictions = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllEventsQuery
            {
                Sport = sport,
                League = league,
                Date = date,
                Status = status,
                HasExpertPredictions = hasExpertPredictions,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PagedResult<EventListDto>>
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
            _logger.LogError(ex, "Unexpected error while fetching events");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching events"
                }
            });
        }
    }

    /// <summary>
    /// Get event detail by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Rebet.Application.DTOs.EventDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetEventDetailQuery
            {
                EventId = id
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<Rebet.Application.DTOs.EventDetailDto>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Event not found: {EventId}", id);
            return NotFound(new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching event detail");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching event detail"
                }
            });
        }
    }

    /// <summary>
    /// Get top game of the day
    /// </summary>
    [HttpGet("top-game")]
    [ProducesResponseType(typeof(ApiResponse<Rebet.Application.Queries.Event.TopGameDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTopGameOfDay(
        [FromQuery] string? sport = null,
        [FromQuery] string? date = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetTopGameOfDayQuery
            {
                Sport = sport,
                Date = date
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<Rebet.Application.Queries.Event.TopGameDto>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Top game not found: {Message}", ex.Message);
            return NotFound(new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "NOT_FOUND",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching top game");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching top game"
                }
            });
        }
    }
}

