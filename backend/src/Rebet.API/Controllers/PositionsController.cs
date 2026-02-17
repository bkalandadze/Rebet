using Rebet.API.Common;
using Rebet.Application.Commands.Position;
using Rebet.Application.DTOs;
using Rebet.Application.Queries.Position;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/positions")]
[ApiVersion("1.0")]
public class PositionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(IMediator mediator, ILogger<PositionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get top positions with filtering and sorting
    /// </summary>
    [HttpGet("top")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PositionListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopPositions(
        [FromQuery] string type,
        [FromQuery] string? sport = null,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "upvotes",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID if authenticated (optional for this endpoint)
            Guid? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var query = new GetTopPositionsQuery
            {
                Type = type,
                Sport = sport,
                Status = status,
                SortBy = sortBy,
                Page = page,
                PageSize = pageSize,
                UserId = userId
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PagedResult<PositionListDto>>
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
            _logger.LogError(ex, "Unexpected error while fetching top positions");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching positions"
                }
            });
        }
    }

    /// <summary>
    /// Get position detail by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PositionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPositionDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID if authenticated (optional for this endpoint)
            Guid? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var query = new GetPositionDetailQuery
            {
                PositionId = id,
                UserId = userId
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PositionDetailDto>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Position not found: {PositionId}", id);
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
            _logger.LogError(ex, "Unexpected error while fetching position detail");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching position detail"
                }
            });
        }
    }

    /// <summary>
    /// Create a new position
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PositionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePosition(
        [FromBody] CreatePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new ApiErrorResponse
                {
                    Success = false,
                    Error = new ErrorDetail
                    {
                        Code = "UNAUTHORIZED",
                        Message = "User ID not found in token"
                    }
                });
            }

            var command = new CreatePositionCommand
            {
                SportEventId = request.SportEventId,
                Market = request.Market,
                Selection = request.Selection,
                Odds = request.Odds,
                Analysis = request.Analysis,
                CreatorId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<PositionDto>
            {
                Success = true,
                Data = result,
                Message = "Position created successfully"
            };

            return CreatedAtAction(nameof(GetPositionDetail), new { id = result.Id }, response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed during position creation");
            var errorDetails = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Validation failed",
                    Details = errorDetails
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Position creation failed: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during position creation");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during position creation"
                }
            });
        }
    }

    /// <summary>
    /// Vote on a position (upvote or downvote)
    /// </summary>
    [HttpPost("{id}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<VoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteOnPosition(
        Guid id,
        [FromBody] VoteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new ApiErrorResponse
                {
                    Success = false,
                    Error = new ErrorDetail
                    {
                        Code = "UNAUTHORIZED",
                        Message = "User ID not found in token"
                    }
                });
            }

            var command = new VotePositionCommand
            {
                PositionId = id,
                VoteType = request.VoteType,
                UserId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<VoteResponse>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Position not found: {PositionId}", id);
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid vote request: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during voting");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during voting"
                }
            });
        }
    }
}

// Request DTOs
public class CreatePositionRequest
{
    public Guid SportEventId { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
}

public class VoteRequest
{
    public int VoteType { get; set; } // 1 = Upvote, 2 = Downvote
}

