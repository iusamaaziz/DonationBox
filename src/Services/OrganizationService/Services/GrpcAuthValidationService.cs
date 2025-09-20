using System.Net.Http.Json;
using System.Text.Json;

namespace OrganizationService.Services;

public class GrpcAuthValidationService : IAuthValidationService
{
    private readonly ILogger<GrpcAuthValidationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _authServiceUrl;

    public GrpcAuthValidationService(ILogger<GrpcAuthValidationService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _authServiceUrl = configuration.GetSection("services:authService:https:0").Value!;
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
            // Use HTTP client to call AuthService instead of gRPC
            var request = new { token };
            var response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/api/auth/validate", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>();
                return result;
            }
            else
            {
                _logger.LogWarning("AuthService returned error status: {StatusCode}", response.StatusCode);
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = "Authentication service error"
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
