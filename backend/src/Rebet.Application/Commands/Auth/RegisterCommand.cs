using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Auth;

public class RegisterCommand : IRequest<AuthResponse>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string Country { get; set; } = null!;
}

