using Rebet.API.Common;
using Rebet.Application.Commands.Auth;
using Rebet.Application.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Rebet.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var authResponse = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<RegisterResponse>
            {
                Success = true,
                Data = new RegisterResponse
                {
                    UserId = authResponse.User.Id,
                    Email = authResponse.User.Email,
                    DisplayName = authResponse.User.DisplayName
                },
                Message = "Registration successful. Please verify your email."
            };

            return CreatedAtAction(nameof(Register), response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed during registration");
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
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
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
            _logger.LogError(ex, "Unexpected error during registration");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during registration"
                }
            });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var authResponse = await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresIn = (int)(authResponse.ExpiresAt - DateTime.UtcNow).TotalSeconds,
                    User = new UserInfoDto
                    {
                        Id = authResponse.User.Id,
                        Email = authResponse.User.Email,
                        DisplayName = authResponse.User.DisplayName,
                        Role = authResponse.User.Role
                    }
                }
            };

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed during login");
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "UNAUTHORIZED",
                    Message = "Invalid email or password"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during login"
                }
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // According to API spec, refresh endpoint returns data directly without success wrapper
            var response = await _mediator.Send(command, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "UNAUTHORIZED",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during token refresh"
                }
            });
        }
    }

    /// <summary>
    /// Logout and invalidate refresh token
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand? command, CancellationToken cancellationToken)
    {
        try
        {
            command ??= new LogoutCommand();
            await _mediator.Send(command, cancellationToken);

            var response = new ApiResponse<object>
            {
                Success = true,
                Message = "Logout successful"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            return StatusCode(500, new ApiErrorResponse
            {
                Success = false,
                Error = new ErrorDetail
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred during logout"
                }
            });
        }
    }
}

// Response DTOs
public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public UserInfoDto User { get; set; } = null!;
}

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = null!;
}

