namespace AuthService.DTOs;

/// <summary>
/// Response model for token validation
/// </summary>
public class ValidateTokenResponse
{
    /// <summary>
    /// Whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// User ID if token is valid
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User's email if token is valid
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's full name if token is valid
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Error message if token is invalid
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Token expiration time if valid
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
