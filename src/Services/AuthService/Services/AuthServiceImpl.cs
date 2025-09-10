using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Google.Apis.Auth;

namespace AuthService.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthServiceImpl : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthServiceImpl> _logger;
    private readonly int _refreshTokenExpirationDays;

    public AuthServiceImpl(
        AuthDbContext context,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthServiceImpl> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
        _refreshTokenExpirationDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 30);
    }

    public async Task<AuthenticationResponse?> AuthenticateAsync(AuthenticationRequest request)
    {
        try
        {
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null || user.Provider != "Local" || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed for email: {Email} - User not found or not local provider", request.Email);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication failed for email: {Email} - User account is inactive", request.Email);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed for email: {Email} - Invalid password", request.Email);
                return null;
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GenerateAuthenticationResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for email: {Email}", request.Email);
            return null;
        }
    }

    public async Task<AuthenticationResponse?> RegisterAsync(RegistrationRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed for email: {Email} - User already exists", request.Email);
                return null;
            }

            // Create new user
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Provider = "Local",
                EmailVerified = false, // In a real system, you'd send a verification email
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            return await GenerateAuthenticationResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return null;
        }
    }

    public async Task<AuthenticationResponse?> AuthenticateWithGoogleAsync(string googleToken)
    {
        try
        {
            var googleClientId = _configuration["GoogleAuth:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                _logger.LogError("Google ClientId not configured");
                return null;
            }

            // Validate Google token
            var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            });

            if (payload == null)
            {
                _logger.LogWarning("Invalid Google token provided");
                return null;
            }

            // Check if user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == payload.Email && u.Provider == "Google");

            if (user == null)
            {
                // Create new user from Google profile
                user = new User
                {
                    Email = payload.Email,
                    FirstName = payload.GivenName ?? "",
                    LastName = payload.FamilyName ?? "",
                    Provider = "Google",
                    ProviderId = payload.Subject,
                    ProfilePictureUrl = payload.Picture,
                    EmailVerified = payload.EmailVerified,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                _logger.LogInformation("New Google user created: {Email}", payload.Email);
            }
            else
            {
                // Update existing user's last login time and profile info
                user.LastLoginAt = DateTime.UtcNow;
                user.ProfilePictureUrl = payload.Picture;
                user.EmailVerified = payload.EmailVerified;
                user.UpdatedAt = DateTime.UtcNow;

                if (!user.IsActive)
                {
                    _logger.LogWarning("Google authentication failed for email: {Email} - User account is inactive", payload.Email);
                    return null;
                }
            }

            await _context.SaveChangesAsync();

            return await GenerateAuthenticationResponseAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google authentication");
            return null;
        }
    }

    public async Task<AuthenticationResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogWarning("Invalid or expired refresh token provided");
                return null;
            }

            if (!refreshToken.User.IsActive)
            {
                _logger.LogWarning("Refresh token failed - User account is inactive: {UserId}", refreshToken.UserId);
                return null;
            }

            // Revoke old refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            // Update user's last login time
            refreshToken.User.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GenerateAuthenticationResponseAsync(refreshToken.User);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request)
    {
        try
        {
            var principal = _jwtService.ValidateToken(request.Token);
            if (principal == null)
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token"
                };
            }

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid user ID in token"
                };
            }

            var user = await GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = "User not found or inactive"
                };
            }

            var expiration = _jwtService.GetTokenExpiration(request.Token);

            return new ValidateTokenResponse
            {
                IsValid = true,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                ExpiresAt = expiration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return new ValidateTokenResponse
            {
                IsValid = false,
                ErrorMessage = "Token validation error"
            };
        }
    }

    public async Task<bool> RevokeTokenAsync(string token, string? ipAddress = null)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null || refreshToken.IsRevoked)
            {
                return false;
            }

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return false;
        }
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    private async Task<AuthenticationResponse> GenerateAuthenticationResponseAsync(User user)
    {
        // Generate JWT access token
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        // Create and save refresh token
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var expiration = _jwtService.GetTokenExpiration(accessToken) ?? DateTime.UtcNow.AddMinutes(15);

        return new AuthenticationResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = expiration,
            Provider = user.Provider,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}
