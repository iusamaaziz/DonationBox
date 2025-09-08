namespace DonationService.Services;

/// <summary>
/// Interface for validating authentication tokens with the AuthService
/// </summary>
public interface IAuthValidationService
{
    /// <summary>
    /// Validate a JWT token with the AuthService
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Validation result with user information</returns>
    Task<AuthValidationResult> ValidateTokenAsync(string token);

    /// <summary>
    /// Extract user information from Authorization header
    /// </summary>
    /// <param name="authorizationHeader">Authorization header value</param>
    /// <returns>User information if valid, null otherwise</returns>
    Task<AuthValidationResult?> GetUserFromAuthHeaderAsync(string? authorizationHeader);
}

/// <summary>
/// Result of token validation
/// </summary>
public class AuthValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
