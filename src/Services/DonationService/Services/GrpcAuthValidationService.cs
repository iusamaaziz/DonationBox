using Grpc.Net.Client;
using AuthService.Grpc;

namespace DonationService.Services;

/// <summary>
/// gRPC-based service for validating authentication tokens with the AuthService
/// </summary>
public class GrpcAuthValidationService : IAuthValidationService
{
    private readonly GrpcChannel _channel;
    private readonly AuthenticationService.AuthenticationServiceClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GrpcAuthValidationService> _logger;

    public GrpcAuthValidationService(
        IConfiguration configuration,
        ILogger<GrpcAuthValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var authServiceUrl = _configuration["AuthService:GrpcUrl"] ?? "https://localhost:7002";
        _channel = GrpcChannel.ForAddress(authServiceUrl);
        _client = new AuthenticationService.AuthenticationServiceClient(_channel);
    }

    public async Task<AuthValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogDebug("Validating token via gRPC");

            var request = new ValidateTokenRequest { Token = token };
            var response = await _client.ValidateTokenAsync(request);

            var result = new AuthValidationResult
            {
                IsValid = response.IsValid,
                ErrorMessage = response.ErrorMessage
            };

            if (response.IsValid)
            {
                if (Guid.TryParse(response.UserId, out var userId))
                {
                    result.UserId = userId;
                }
                result.Email = response.Email;
                result.FullName = response.FullName;
                
                if (response.ExpiresAt > 0)
                {
                    result.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresAt).DateTime;
                }
            }

            _logger.LogDebug("Token validation result: {IsValid}", response.IsValid);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via gRPC");
            return new AuthValidationResult
            {
                IsValid = false,
                ErrorMessage = "Unable to connect to AuthService via gRPC"
            };
        }
    }

    public async Task<AuthValidationResult?> GetUserFromAuthHeaderAsync(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return null;
        }

        // Extract token from "Bearer {token}" format
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid authorization header format"
            };
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return new AuthValidationResult
            {
                IsValid = false,
                ErrorMessage = "Missing token in authorization header"
            };
        }

        return await ValidateTokenAsync(token);
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
