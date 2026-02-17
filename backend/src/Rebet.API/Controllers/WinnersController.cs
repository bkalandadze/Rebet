using Rebet.API.Common;
using Rebet.Application.DTOs;
using Rebet.Application.Queries.Winner;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/winners")]
[ApiVersion("1.0")]
public class WinnersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WinnersController> _logger;

    public WinnersController(IMediator mediator, ILogger<WinnersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get winners with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Rebet.Application.Queries.Winner.WinnerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWinners(
        [FromQuery] string? sport = null,
        [FromQuery] string? period = null,
        [FromQuery] decimal? minOdds = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetWinnersQuery
            {
                Sport = sport,
                Period = period,
                MinOdds = minOdds,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PagedResult<Rebet.Application.Queries.Winner.WinnerDto>>
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
            _logger.LogError(ex, "Unexpected error while fetching winners");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching winners"
                }
            });
        }
    }
}

