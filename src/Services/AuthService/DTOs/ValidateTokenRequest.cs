using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

/// <summary>
/// Request model for token validation
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// The JWT access token to validate
    /// </summary>
    [Required(ErrorMessage = "Token is required")]
    public string Token { get; set; } = string.Empty;
}
