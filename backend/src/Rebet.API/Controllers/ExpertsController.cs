using Rebet.API.Common;
using Rebet.Application.Commands.Expert;
using Rebet.Application.DTOs;
using Rebet.Application.Queries.Expert;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/experts")]
[ApiVersion("1.0")]
public class ExpertsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExpertsController> _logger;

    public ExpertsController(IMediator mediator, ILogger<ExpertsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get expert leaderboard with filtering and sorting
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ExpertListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExpertLeaderboard(
        [FromQuery] string? sortBy = null,
        [FromQuery] string? specialization = null,
        [FromQuery] decimal? minWinRate = null,
        [FromQuery] int? tier = null,
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

            var query = new GetExpertLeaderboardQuery
            {
                SortBy = sortBy ?? "winRate",
                Specialization = specialization,
                MinWinRate = minWinRate,
                Tier = tier,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PagedResult<ExpertListDto>>
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
            _logger.LogError(ex, "Unexpected error while fetching expert leaderboard");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching expert leaderboard"
                }
            });
        }
    }

    /// <summary>
    /// Get expert profile by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ExpertProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpertProfile(
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

            var query = new GetExpertProfileQuery
            {
                ExpertId = id,
                UserId = userId
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<ExpertProfileDto>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Expert not found: {ExpertId}", id);
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
            _logger.LogError(ex, "Unexpected error while fetching expert profile");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching expert profile"
                }
            });
        }
    }

    /// <summary>
    /// Subscribe or unsubscribe to an expert (toggle)
    /// </summary>
    [HttpPost("{id}/subscribe")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Rebet.Application.Commands.Expert.SubscribeToExpertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubscribeToExpert(
        Guid id,
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

            var command = new SubscribeToExpertCommand
            {
                ExpertId = id,
                UserId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<Rebet.Application.Commands.Expert.SubscribeToExpertResponse>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Expert or user not found: {Message}", ex.Message);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Subscription failed: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during subscription");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during subscription"
                }
            });
        }
    }

    /// <summary>
    /// Vote on an expert (upvote or downvote)
    /// </summary>
    [HttpPost("{id}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Rebet.Application.Commands.Expert.VoteExpertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteOnExpert(
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

            var command = new VoteExpertCommand
            {
                ExpertId = id,
                VoteType = request.VoteType,
                UserId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<Rebet.Application.Commands.Expert.VoteExpertResponse>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Expert not found: {ExpertId}", id);
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

