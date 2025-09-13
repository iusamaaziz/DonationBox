using Grpc.Core;
using AuthService.Grpc;
using AuthService.Services;

namespace AuthService.Services;

/// <summary>
/// gRPC implementation of the authentication service
/// </summary>
public class GrpcAuthenticationService : Grpc.AuthenticationService.AuthenticationServiceBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<GrpcAuthenticationService> _logger;

    public GrpcAuthenticationService(IAuthService authService, ILogger<GrpcAuthenticationService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Validate a JWT token via gRPC
    /// </summary>
    public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Validating token via gRPC");

            var validationRequest = new DTOs.ValidateTokenRequest { Token = request.Token };
            var result = await _authService.ValidateTokenAsync(validationRequest);

            var response = new ValidateTokenResponse
            {
                IsValid = result.IsValid,
                ErrorMessage = result.ErrorMessage ?? string.Empty
            };

            if (result.IsValid && result.UserId.HasValue)
            {
                response.UserId = result.UserId.Value.ToString();
                response.Email = result.Email ?? string.Empty;
                response.FullName = result.FullName ?? string.Empty;
                
                if (result.ExpiresAt.HasValue)
                {
                    response.ExpiresAt = ((DateTimeOffset)result.ExpiresAt.Value).ToUnixTimeSeconds();
                }
            }

            _logger.LogDebug("Token validation result: {IsValid}", result.IsValid);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via gRPC");
            return new ValidateTokenResponse
            {
                IsValid = false,
                ErrorMessage = "Internal server error during token validation"
            };
        }
    }

    /// <summary>
    /// Get user by ID via gRPC
    /// </summary>
    public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Getting user by ID via gRPC: {UserId}", request.UserId);

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return new GetUserResponse
                {
                    Found = false,
                    ErrorMessage = "Invalid user ID format"
                };
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new GetUserResponse
                {
                    Found = false,
                    ErrorMessage = "User not found"
                };
            }

            return new GetUserResponse
            {
                Found = true,
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Provider = user.Provider,
                ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty,
                EmailVerified = user.EmailVerified,
                IsActive = user.IsActive,
                CreatedAt = ((DateTimeOffset)user.CreatedAt).ToUnixTimeSeconds(),
                LastLoginAt = user.LastLoginAt.HasValue ? ((DateTimeOffset)user.LastLoginAt.Value).ToUnixTimeSeconds() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID via gRPC: {UserId}", request.UserId);
            return new GetUserResponse
            {
                Found = false,
                ErrorMessage = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Get user by email via gRPC
    /// </summary>
    public override async Task<GetUserResponse> GetUserByEmail(GetUserByEmailRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Getting user by email via gRPC: {Email}", request.Email);

            var user = await _authService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new GetUserResponse
                {
                    Found = false,
                    ErrorMessage = "User not found"
                };
            }

            return new GetUserResponse
            {
                Found = true,
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Provider = user.Provider,
                ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty,
                EmailVerified = user.EmailVerified,
                IsActive = user.IsActive,
                CreatedAt = ((DateTimeOffset)user.CreatedAt).ToUnixTimeSeconds(),
                LastLoginAt = user.LastLoginAt.HasValue ? ((DateTimeOffset)user.LastLoginAt.Value).ToUnixTimeSeconds() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email via gRPC: {Email}", request.Email);
            return new GetUserResponse
            {
                Found = false,
                ErrorMessage = "Internal server error"
            };
        }
    }
}
