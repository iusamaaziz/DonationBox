using AuthService.DTOs;

namespace AuthService.Services;

/// <summary>
/// Token validation service implementation
/// </summary>
public class TokenValidationService : ITokenValidationService
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<TokenValidationService> _logger;

    public TokenValidationService(
        IAuthService authService,
        IJwtService jwtService,
        ILogger<TokenValidationService> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string token)
    {
        var request = new ValidateTokenRequest { Token = token };
        return await _authService.ValidateTokenAsync(request);
    }

    public async Task<(Guid? UserId, string? Email, string? FullName)> GetUserFromTokenAsync(string token)
    {
        try
        {
            var validationResponse = await ValidateTokenAsync(token);
            if (validationResponse.IsValid)
            {
                return (validationResponse.UserId, validationResponse.Email, validationResponse.FullName);
            }

            return (null, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user from token");
            return (null, null, null);
        }
    }
}
