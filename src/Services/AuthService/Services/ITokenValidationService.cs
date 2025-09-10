using AuthService.DTOs;

namespace AuthService.Services;

/// <summary>
/// Interface for token validation service that can be called by other microservices
/// </summary>
public interface ITokenValidationService
{
    /// <summary>
    /// Validate a token via HTTP request (for other microservices)
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Validation response</returns>
    Task<ValidateTokenResponse> ValidateTokenAsync(string token);

    /// <summary>
    /// Extract user information from a valid token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User information if valid, null otherwise</returns>
    Task<(Guid? UserId, string? Email, string? FullName)> GetUserFromTokenAsync(string token);
}
