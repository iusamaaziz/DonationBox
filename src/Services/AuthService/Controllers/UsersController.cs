using Microsoft.AspNetCore.Mvc;
using AuthService.Services;
using System.Net;

namespace AuthService.Controllers;

/// <summary>
/// Users controller for user management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IAuthService authService, ILogger<UsersController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User information</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var response = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.Provider,
                user.ProfilePictureUrl,
                user.EmailVerified,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User information</returns>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            var user = await _authService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var response = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.Provider,
                user.ProfilePictureUrl,
                user.EmailVerified,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }
}
