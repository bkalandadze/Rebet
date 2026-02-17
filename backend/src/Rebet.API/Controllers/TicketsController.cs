using Rebet.API.Common;
using Rebet.Application.Commands.Ticket;
using Rebet.Application.DTOs;
using Rebet.Application.Queries.Ticket;
using Rebet.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/tickets")]
[ApiVersion("1.0")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(IMediator mediator, ILogger<TicketsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get top tickets with filtering and sorting
    /// </summary>
    [HttpGet("top")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TicketListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopTickets(
        [FromQuery] string type,
        [FromQuery] string? sport = null,
        [FromQuery] string? status = null,
        [FromQuery] decimal? minOdds = null,
        [FromQuery] string sortBy = "odds",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetTopTicketsQuery
            {
                Type = type,
                Sport = sport,
                Status = status,
                MinOdds = minOdds,
                SortBy = sortBy,
                Page = page,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<PagedResult<TicketListDto>>
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
            _logger.LogError(ex, "Unexpected error while fetching top tickets");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching tickets"
                }
            });
        }
    }

    /// <summary>
    /// Get ticket detail by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketDetail(
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

            var query = new GetTicketDetailQuery
            {
                TicketId = id,
                UserId = userId
            };

            var result = await _mediator.Send(query, cancellationToken);

            var response = new ApiResponse<TicketDetailDto>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ticket not found: {TicketId}", id);
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
            _logger.LogError(ex, "Unexpected error while fetching ticket detail");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while fetching ticket detail"
                }
            });
        }
    }

    /// <summary>
    /// Create a new ticket
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TicketDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request,
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

            var command = new CreateTicketCommand
            {
                ExpertId = userId, // Assuming the creator is the expert
                Title = request.Title,
                Description = request.Description,
                Type = (TicketType)request.Type,
                Stake = request.Stake,
                Visibility = (TicketVisibility)request.Visibility,
                Entries = request.Entries.Select(e => new TicketEntryDto
                {
                    SportEventId = e.SportEventId,
                    Market = e.Market,
                    Selection = e.Selection,
                    Odds = e.Odds,
                    Analysis = e.Analysis
                }).ToList()
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<TicketDto>
            {
                Success = true,
                Data = result,
                Message = "Ticket created successfully"
            };

            return CreatedAtAction(nameof(GetTicketDetail), new { id = result.Id }, response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed during ticket creation");
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
            _logger.LogWarning(ex, "Ticket creation failed: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during ticket creation");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during ticket creation"
                }
            });
        }
    }

    /// <summary>
    /// Follow or unfollow a ticket (toggle)
    /// </summary>
    [HttpPost("{id}/follow")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Rebet.Application.Commands.Ticket.FollowTicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowTicket(
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

            var command = new FollowTicketCommand
            {
                TicketId = id,
                UserId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<Rebet.Application.Commands.Ticket.FollowTicketResponse>
            {
                Success = true,
                Data = result
            };

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ticket or user not found: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during follow/unfollow");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during follow/unfollow"
                }
            });
        }
    }

    /// <summary>
    /// Add a comment to a ticket
    /// </summary>
    [HttpPost("{id}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddCommentRequest request,
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

            var command = new AddCommentCommand
            {
                TicketId = id,
                Content = request.Content,
                ParentCommentId = request.ParentCommentId,
                UserId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<CommentDto>
            {
                Success = true,
                Data = result,
                Message = "Comment added successfully"
            };

            return CreatedAtAction(nameof(GetTicketDetail), new { id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ticket or parent comment not found: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during comment creation");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during comment creation"
                }
            });
        }
    }
}

// Request DTOs
public class CreateTicketRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Type { get; set; } // 1=Single, 2=Multi, 3=System
    public decimal Stake { get; set; }
    public int Visibility { get; set; } = 1; // 1=Public, 2=SubscribersOnly, 3=Private
    public List<TicketEntryRequest> Entries { get; set; } = new();
}

public class TicketEntryRequest
{
    public Guid SportEventId { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
}

public class AddCommentRequest
{
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
}


