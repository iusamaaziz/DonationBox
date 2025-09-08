using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

/// <summary>
/// Request model for refreshing access tokens
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
