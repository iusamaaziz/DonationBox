using Grpc.Net.Client;
using AuthService.Grpc;

namespace DonorService.Services;

/// <summary>
/// gRPC client for communicating with AuthService
/// </summary>
public class AuthGrpcClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly AuthenticationService.AuthenticationServiceClient _client;
    private readonly ILogger<AuthGrpcClient> _logger;

    public AuthGrpcClient(string authServiceUrl, ILogger<AuthGrpcClient> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(authServiceUrl);
        _client = new AuthenticationService.AuthenticationServiceClient(_channel);
    }

    /// <summary>
    /// Validate a JWT token via AuthService
    /// </summary>
    public async Task<(bool IsValid, string? UserId, string? Email, string? ErrorMessage)> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogDebug("Validating token via AuthService gRPC");

            var request = new ValidateTokenRequest { Token = token };
            var response = await _client.ValidateTokenAsync(request);

            _logger.LogDebug("Token validation result: {IsValid}", response.IsValid);

            return (
                response.IsValid,
                response.UserId,
                response.Email,
                response.ErrorMessage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via AuthService gRPC");
            return (false, null, null, "AuthService communication error");
        }
    }

    /// <summary>
    /// Get user information by ID via AuthService
    /// </summary>
    public async Task<(bool Found, string? UserId, string? Email, string? FirstName, string? LastName, string? ErrorMessage)> GetUserByIdAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting user by ID via AuthService gRPC: {UserId}", userId);

            var request = new GetUserRequest { UserId = userId };
            var response = await _client.GetUserAsync(request);

            if (!response.Found)
            {
                _logger.LogDebug("User not found via AuthService: {UserId}", userId);
                return (false, null, null, null, null, response.ErrorMessage);
            }

            return (
                true,
                response.UserId,
                response.Email,
                response.FirstName,
                response.LastName,
                null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID via AuthService gRPC: {UserId}", userId);
            return (false, null, null, null, null, "AuthService communication error");
        }
    }

    /// <summary>
    /// Get user information by email via AuthService
    /// </summary>
    public async Task<(bool Found, string? UserId, string? Email, string? FirstName, string? LastName, string? ErrorMessage)> GetUserByEmailAsync(string email)
    {
        try
        {
            _logger.LogDebug("Getting user by email via AuthService gRPC: {Email}", email);

            var request = new GetUserByEmailRequest { Email = email };
            var response = await _client.GetUserByEmailAsync(request);

            if (!response.Found)
            {
                _logger.LogDebug("User not found via AuthService: {Email}", email);
                return (false, null, null, null, null, response.ErrorMessage);
            }

            return (
                true,
                response.UserId,
                response.Email,
                response.FirstName,
                response.LastName,
                null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email via AuthService gRPC: {Email}", email);
            return (false, null, null, null, null, "AuthService communication error");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
