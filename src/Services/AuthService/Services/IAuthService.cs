using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Services;

/// <summary>
/// Interface for authentication service operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate a user with email and password
    /// </summary>
    /// <param name="request">Authentication request</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> AuthenticateAsync(AuthenticationRequest request);

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> RegisterAsync(RegistrationRequest request);

    /// <summary>
    /// Authenticate a user with Google OAuth
    /// </summary>
    /// <param name="googleToken">Google OAuth token</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> AuthenticateWithGoogleAsync(string googleToken);

    /// <summary>
    /// Refresh an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with tokens</returns>
    Task<AuthenticationResponse?> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Validate a JWT token
    /// </summary>
    /// <param name="request">Token validation request</param>
    /// <returns>Token validation response</returns>
    Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request);

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="token">Refresh token to revoke</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>True if successful</returns>
    Task<bool> RevokeTokenAsync(string token, string? ipAddress = null);

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User entity</returns>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Get user by email
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User entity</returns>
    Task<User?> GetUserByEmailAsync(string email);
}
