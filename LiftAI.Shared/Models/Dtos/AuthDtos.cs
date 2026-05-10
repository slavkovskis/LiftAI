using System.ComponentModel.DataAnnotations;

namespace LiftAI.Shared.Models.Dtos;

public class RegisterDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? Email { get; set; }
    public bool IsPremium { get; set; }
}