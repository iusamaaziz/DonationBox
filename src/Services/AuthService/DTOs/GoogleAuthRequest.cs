using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs;

/// <summary>
/// Request model for Google OAuth authentication
/// </summary>
public class GoogleAuthRequest
{
    /// <summary>
    /// Google OAuth token
    /// </summary>
    [Required(ErrorMessage = "Google token is required")]
    public string GoogleToken { get; set; } = string.Empty;
}
