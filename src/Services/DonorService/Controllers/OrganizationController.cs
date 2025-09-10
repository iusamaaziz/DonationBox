using Microsoft.AspNetCore.Mvc;
using DonorService.Services;
using DonorService.DTOs;
using DonorService.Models;

namespace DonorService.Controllers;

/// <summary>
/// REST API controller for organization management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(IOrganizationService organizationService, ILogger<OrganizationController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new welfare organization
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <returns>Created organization details</returns>
    /// <response code="201">Returns the newly created organization</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating organization: {Name} for user: {UserId}", request.Name, request.CreatedByUserId);

            var organization = await _organizationService.CreateOrganizationAsync(
                request.Name,
                request.Description,
                request.Type,
                request.Mission,
                request.WebsiteUrl,
                request.ContactEmail,
                request.ContactPhone,
                request.Address,
                request.TaxId,
                request.CreatedByUserId);

            var organizationDto = MapToOrganizationDto(organization);
            return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organizationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization: {Name} for user: {UserId}", request.Name, request.CreatedByUserId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details</returns>
    /// <response code="200">Returns the organization</response>
    /// <response code="404">Organization not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting organization: {OrganizationId}", id);

            var organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                _logger.LogWarning("Organization not found: {OrganizationId}", id);
                return NotFound(new { message = "Organization not found" });
            }

            var organizationDto = MapToOrganizationDto(organization);
            return Ok(organizationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization: {OrganizationId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update organization details
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Organization update request</param>
    /// <returns>Updated organization details</returns>
    /// <response code="200">Returns the updated organization</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">Access denied</response>
    /// <response code="404">Organization not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Updating organization: {OrganizationId} by user: {UserId}", id, request.UpdatedByUserId);

            var organization = await _organizationService.UpdateOrganizationAsync(
                id,
                request.Name,
                request.Description,
                request.Type,
                request.Mission,
                request.WebsiteUrl,
                request.ContactEmail,
                request.ContactPhone,
                request.Address,
                request.TaxId,
                request.UpdatedByUserId);

            if (organization == null)
            {
                return NotFound(new { message = "Organization not found or access denied" });
            }

            var organizationDto = MapToOrganizationDto(organization);
            return Ok(organizationDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied for organization update: {OrganizationId}", id);
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization: {OrganizationId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all organizations with pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="filterType">Optional filter by organization type</param>
    /// <returns>Paginated list of organizations</returns>
    /// <response code="200">Returns paginated organization list</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetOrganizationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrganizations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filterType = null)
    {
        try
        {
            _logger.LogInformation("Getting organizations - Page: {Page}, Size: {Size}, Filter: {Filter}",
                page, pageSize, filterType);

            var (organizations, totalCount) = await _organizationService.GetAllOrganizationsAsync(page, pageSize, filterType);
            var organizationDtos = organizations.Select(MapToOrganizationDto);

            var response = new GetOrganizationsResponse
            {
                Organizations = organizationDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate organization access for a user
    /// </summary>
    /// <param name="request">Access validation request</param>
    /// <returns>Access validation result</returns>
    /// <response code="200">Returns access validation result</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("validate-access")]
    [ProducesResponseType(typeof(ValidateOrganizationAccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateOrganizationAccess([FromBody] ValidateOrganizationAccessRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!Guid.TryParse(request.OrganizationId, out var organizationId))
            {
                return BadRequest(new { message = "Invalid organization ID format" });
            }

            _logger.LogInformation("Validating access for user {UserId} to organization {OrganizationId}",
                request.UserId, organizationId);

            var hasAccess = await _organizationService.ValidateOrganizationAccessAsync(organizationId, request.UserId);

            var response = new ValidateOrganizationAccessResponse
            {
                HasAccess = hasAccess
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating organization access for user {UserId} to organization {OrganizationId}",
                request.UserId, request.OrganizationId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete organization (soft delete)
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="userId">User ID requesting deletion</param>
    /// <returns>Deletion result</returns>
    /// <response code="204">Organization deleted successfully</response>
    /// <response code="403">Access denied</response>
    /// <response code="404">Organization not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrganization(Guid id, [FromQuery] string userId)
    {
        try
        {
            _logger.LogInformation("Deleting organization: {OrganizationId} by user: {UserId}", id, userId);

            var success = await _organizationService.DeleteOrganizationAsync(id, userId);

            if (!success)
            {
                return NotFound(new { message = "Organization not found" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied for organization deletion: {OrganizationId}", id);
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organization: {OrganizationId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Map WelfareOrganization model to OrganizationDto
    /// </summary>
    private static OrganizationDto MapToOrganizationDto(WelfareOrganization organization)
    {
        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            Type = organization.Type,
            Mission = organization.Mission,
            WebsiteUrl = organization.WebsiteUrl,
            ContactEmail = organization.ContactEmail,
            ContactPhone = organization.ContactPhone,
            Address = organization.Address,
            TaxId = organization.TaxId,
            CreatedByUserId = organization.CreatedByUserId,
            IsVerified = organization.IsVerified,
            IsActive = organization.IsActive,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }
}
