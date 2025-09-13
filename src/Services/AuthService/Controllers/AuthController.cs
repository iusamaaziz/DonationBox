using Microsoft.AspNetCore.Mvc;
using AuthService.DTOs;
using AuthService.Services;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Controllers;

/// <summary>
/// Authentication controller for user login, registration, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate a user with email and password
    /// </summary>
    /// <param name="request">Authentication request</param>
    /// <returns>Authentication response with JWT tokens</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.AuthenticateAsync(request);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            _logger.LogInformation("User authenticated successfully: {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during authentication" });
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response with JWT tokens</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthenticationResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RegisterAsync(request);
            if (response == null)
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return CreatedAtAction(nameof(Login), new { }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Authenticate with Google OAuth
    /// </summary>
    /// <param name="request">Google token request</param>
    /// <returns>Authentication response with JWT tokens</returns>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthenticationResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.AuthenticateWithGoogleAsync(request.GoogleToken);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }

            _logger.LogInformation("User authenticated with Google successfully: {Email}", response.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google authentication");
            return StatusCode(500, new { message = "An error occurred during Google authentication" });
        }
    }

    /// <summary>
    /// Refresh an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with JWT tokens</returns>
    [Authorize]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.RefreshTokenAsync(request);
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", response.UserId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Validate a JWT access token
    /// </summary>
    /// <param name="request">Token validation request</param>
    /// <returns>Token validation response</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidateTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.ValidateTokenAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return StatusCode(500, new { message = "An error occurred during token validation" });
        }
    }

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>Success response</returns>
    [HttpPost("revoke")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var success = await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);

            if (success)
            {
                _logger.LogInformation("Token revoked successfully from IP: {IpAddress}", ipAddress);
                return Ok(new { message = "Token revoked successfully" });
            }

            return BadRequest(new { message = "Invalid or already revoked token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            return StatusCode(500, new { message = "An error occurred during token revocation" });
        }
    }
}
