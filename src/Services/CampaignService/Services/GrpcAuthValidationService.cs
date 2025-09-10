using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;

namespace CampaignService.Services;

public class GrpcAuthValidationService : IAuthValidationService
{
    private readonly ILogger<GrpcAuthValidationService> _logger;
    private readonly string _authServiceUrl;

    public GrpcAuthValidationService(ILogger<GrpcAuthValidationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _authServiceUrl = configuration["AuthService:Url"] ?? "https://localhost:7181";
    }

    public async Task<ValidateTokenResponse?> GetUserFromAuthHeaderAsync(string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            using var channel = GrpcChannel.ForAddress(_authServiceUrl);
            var client = new Auth.AuthClient(channel);

            var request = new ValidateTokenRequest
            {
                Token = token
            };

            var response = await client.ValidateTokenAsync(request);

            if (response.IsValid)
            {
                return new ValidateTokenResponse
                {
                    IsValid = true,
                    UserId = Guid.Parse(response.UserId),
                    Email = response.Email,
                    FullName = response.FullName
                };
            }
            else
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = response.ErrorMessage
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token with AuthService");
            return new ValidateTokenResponse
            {
                IsValid = false,
                ErrorMessage = "Authentication service unavailable"
            };
        }
    }
}
