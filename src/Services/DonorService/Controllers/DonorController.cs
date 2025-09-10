using Microsoft.AspNetCore.Mvc;
using DonorService.Services;
using DonorService.DTOs;
using DonorService.Models;

namespace DonorService.Controllers;

/// <summary>
/// REST API controller for donor management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DonorController : ControllerBase
{
    private readonly IDonorService _donorService;
    private readonly ILogger<DonorController> _logger;

    public DonorController(IDonorService donorService, ILogger<DonorController> logger)
    {
        _donorService = donorService;
        _logger = logger;
    }

    /// <summary>
    /// Get donor profile by user ID
    /// </summary>
    /// <param name="userId">User ID of the donor</param>
    /// <returns>Donor profile information</returns>
    /// <response code="200">Returns the donor profile</response>
    /// <response code="404">Donor not found</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(DonorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDonor(string userId)
    {
        try
        {
            _logger.LogInformation("Getting donor profile for user: {UserId}", userId);

            var donor = await _donorService.GetDonorByUserIdAsync(userId);
            if (donor == null)
            {
                _logger.LogWarning("Donor not found for user: {UserId}", userId);
                return NotFound(new { message = "Donor not found" });
            }

            var donorDto = MapToDonorDto(donor);
            return Ok(donorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting donor profile for user: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create or update donor profile
    /// </summary>
    /// <param name="request">Donor creation/update request</param>
    /// <returns>Created or updated donor profile</returns>
    /// <response code="200">Returns the updated donor profile</response>
    /// <response code="201">Returns the newly created donor profile</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(DonorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DonorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdateDonor([FromBody] CreateDonorRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating/updating donor profile for user: {UserId}", request.UserId);

            var donor = await _donorService.CreateOrUpdateDonorAsync(
                request.UserId,
                request.Bio,
                request.Interests,
                request.PhoneNumber,
                request.Address);

            var donorDto = MapToDonorDto(donor);

            // Check if this was a new creation or update
            var isNew = donor.CreatedAt == donor.UpdatedAt;
            return isNew ? CreatedAtAction(nameof(GetDonor), new { userId = donor.UserId }, donorDto) : Ok(donorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating donor profile for user: {UserId}", request.UserId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get organizations created by a donor
    /// </summary>
    /// <param name="userId">User ID of the donor</param>
    /// <returns>List of organizations created by the donor</returns>
    /// <response code="200">Returns the list of organizations</response>
    [HttpGet("{userId}/organizations")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDonorOrganizations(string userId)
    {
        try
        {
            _logger.LogInformation("Getting organizations for donor: {UserId}", userId);

            var organizations = await _donorService.GetDonorOrganizationsAsync(userId);
            var organizationDtos = organizations.Select(MapToOrganizationDto);

            return Ok(organizationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations for donor: {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Map Donor model to DonorDto
    /// </summary>
    private static DonorDto MapToDonorDto(Donor donor)
    {
        return new DonorDto
        {
            UserId = donor.UserId,
            Bio = donor.Bio,
            Interests = donor.InterestsList,
            PhoneNumber = donor.PhoneNumber,
            Address = donor.Address,
            IsActive = donor.IsActive,
            CreatedAt = donor.CreatedAt,
            UpdatedAt = donor.UpdatedAt
        };
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
