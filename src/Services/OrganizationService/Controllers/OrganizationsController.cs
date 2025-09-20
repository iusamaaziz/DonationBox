using Microsoft.AspNetCore.Mvc;
using OrganizationService.Attributes;
using OrganizationService.DTOs;
using OrganizationService.Services;

namespace OrganizationService.Controllers;

/// <summary>
/// Controller for managing charity organizations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(
        IOrganizationService organizationService,
        ILogger<OrganizationsController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active organizations (no authentication required)
    /// </summary>
    /// <returns>List of organization summaries</returns>
    [HttpGet]
    public async Task<ActionResult<List<OrganizationSummary>>> GetOrganizations()
    {
        var organizations = await _organizationService.GetOrganizationsAsync();
        return Ok(organizations);
    }

    /// <summary>
    /// Gets an organization by ID (no authentication required)
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationResponse>> GetOrganization(Guid id)
    {
        var organization = await _organizationService.GetOrganizationAsync(id);

        if (organization == null)
        {
            return NotFound();
        }

        return Ok(organization);
    }

    /// <summary>
    /// Gets organizations created by the authenticated user
    /// </summary>
    /// <returns>List of organization summaries created by the user</returns>
    [HttpGet("my-organizations")]
    [Authorize]
    public async Task<ActionResult<List<OrganizationSummary>>> GetMyOrganizations()
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var organizations = await _organizationService.GetOrganizationsByUserAsync(userId.Value.ToString());
        return Ok(organizations);
    }

    /// <summary>
    /// Creates a new organization (requires authentication)
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <returns>Created organization details</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<OrganizationResponse>> CreateOrganization(CreateOrganizationRequest request)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var organization = await _organizationService.CreateOrganizationAsync(request, userId.Value.ToString());

        if (organization == null)
        {
            return BadRequest("Failed to create organization");
        }

        return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
    }

    /// <summary>
    /// Updates an organization (requires authentication and ownership)
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<OrganizationResponse>> UpdateOrganization(Guid id, UpdateOrganizationRequest request)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var organization = await _organizationService.UpdateOrganizationAsync(id, request, userId.Value.ToString());

        if (organization == null)
        {
            return NotFound("Organization not found or you don't have permission to update it");
        }

        return Ok(organization);
    }

    /// <summary>
    /// Deletes an organization (requires authentication and ownership)
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var success = await _organizationService.DeleteOrganizationAsync(id, userId.Value.ToString());

        if (!success)
        {
            return NotFound("Organization not found or you don't have permission to delete it");
        }

        return NoContent();
    }

    /// <summary>
    /// Extracts user ID from HttpContext (set by AuthorizeAttribute)
    /// </summary>
    /// <returns>User ID from context or null if not found</returns>
    private Guid? GetUserIdFromContext()
    {
        if (HttpContext.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is Guid userId)
        {
            return userId;
        }

        return null;
    }
}

