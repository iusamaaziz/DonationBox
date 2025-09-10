using Microsoft.AspNetCore.Mvc;
using DonationService.DTOs;
using DonationService.Models;
using DonationService.Services;
using DonationService.Attributes;
using DonationService.Extensions;

namespace DonationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(ICampaignService campaignService, ILogger<CampaignsController> logger)
    {
        _campaignService = campaignService;
        _logger = logger;
    }

    /// <summary>
    /// Get all donation campaigns
    /// </summary>
    /// <returns>List of all campaigns</returns>
    /// <remarks>
    /// **Public Endpoint**: No authentication required.
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetAllCampaigns()
    {
        try
        {
            var campaigns = await _campaignService.GetAllCampaignsAsync();
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all campaigns");
            return StatusCode(500, "An error occurred while retrieving campaigns");
        }
    }

    /// <summary>
    /// Get all active donation campaigns
    /// </summary>
    /// <returns>List of active campaigns</returns>
    /// <remarks>
    /// **Public Endpoint**: No authentication required.
    /// </remarks>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetActiveCampaigns()
    {
        try
        {
            var campaigns = await _campaignService.GetActiveCampaignsAsync();
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active campaigns");
            return StatusCode(500, "An error occurred while retrieving active campaigns");
        }
    }

    /// <summary>
    /// Get a specific campaign by ID
    /// </summary>
    /// <param name="id">Campaign ID</param>
    /// <returns>Campaign details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CampaignResponse>> GetCampaign(int id)
    {
        try
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null)
            {
                return NotFound($"Campaign with ID {id} not found");
            }

            return Ok(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while retrieving the campaign");
        }
    }

    /// <summary>
    /// Get campaign statistics
    /// </summary>
    /// <param name="id">Campaign ID</param>
    /// <returns>Campaign statistics</returns>
    [HttpGet("{id:int}/stats")]
    public async Task<ActionResult<CampaignStatsResponse>> GetCampaignStats(int id)
    {
        try
        {
            var stats = await _campaignService.GetCampaignStatsAsync(id);
            if (stats == null)
            {
                return NotFound($"Campaign with ID {id} not found");
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaign stats for {CampaignId}", id);
            return StatusCode(500, "An error occurred while retrieving campaign statistics");
        }
    }

    /// <summary>
    /// Create a new donation campaign
    /// </summary>
    /// <param name="request">Campaign creation request</param>
    /// <returns>Created campaign</returns>
    /// <remarks>
    /// **Authentication Required**: This endpoint requires a valid JWT token in the Authorization header.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/campaigns
    ///     {
    ///         "title": "Community Center Renovation",
    ///         "description": "Help us renovate our local community center",
    ///         "goalAmount": 50000,
    ///         "endDate": "2024-12-31T23:59:59Z"
    ///     }
    /// 
    /// </remarks>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CampaignResponse>> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.EndDate <= request.StartDate)
            {
                return BadRequest("End date must be after start date");
            }

            if (request.StartDate < DateTime.UtcNow.Date)
            {
                return BadRequest("Start date cannot be in the past");
            }

            var campaign = await _campaignService.CreateCampaignAsync(request);
            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return StatusCode(500, "An error occurred while creating the campaign");
        }
    }

    /// <summary>
    /// Update an existing campaign
    /// </summary>
    /// <param name="id">Campaign ID</param>
    /// <param name="request">Campaign update request</param>
    /// <returns>Updated campaign</returns>
    /// <remarks>
    /// **Authentication Required**: This endpoint requires a valid JWT token in the Authorization header.
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<CampaignResponse>> UpdateCampaign(int id, [FromBody] UpdateCampaignRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate <= request.StartDate)
            {
                return BadRequest("End date must be after start date");
            }

            var campaign = await _campaignService.UpdateCampaignAsync(id, request);
            if (campaign == null)
            {
                return NotFound($"Campaign with ID {id} not found");
            }

            return Ok(campaign);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while updating the campaign");
        }
    }

    /// <summary>
    /// Delete a campaign
    /// </summary>
    /// <param name="id">Campaign ID</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// **Authentication Required**: This endpoint requires a valid JWT token in the Authorization header.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<ActionResult> DeleteCampaign(int id)
    {
        try
        {
            var result = await _campaignService.DeleteCampaignAsync(id);
            if (!result)
            {
                return NotFound($"Campaign with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while deleting the campaign");
        }
    }

    /// <summary>
    /// Update campaign status
    /// </summary>
    /// <param name="id">Campaign ID</param>
    /// <param name="status">New status</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// **Authentication Required**: This endpoint requires a valid JWT token in the Authorization header.
    /// </remarks>
    [HttpPatch("{id:int}/status")]
    [Authorize]
    public async Task<ActionResult> UpdateCampaignStatus(int id, [FromBody] CampaignStatus status)
    {
        try
        {
            var result = await _campaignService.UpdateCampaignStatusAsync(id, status);
            if (!result)
            {
                return NotFound($"Campaign with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign status for {CampaignId}", id);
            return StatusCode(500, "An error occurred while updating campaign status");
        }
    }

    /// <summary>
    /// Get campaigns by creator
    /// </summary>
    /// <param name="createdBy">Creator identifier</param>
    /// <returns>List of campaigns created by the specified user</returns>
    [HttpGet("creator/{createdBy}")]
    public async Task<ActionResult<IEnumerable<CampaignResponse>>> GetCampaignsByCreator(string createdBy)
    {
        try
        {
            var campaigns = await _campaignService.GetCampaignsByCreatorAsync(createdBy);
            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns for creator {CreatedBy}", createdBy);
            return StatusCode(500, "An error occurred while retrieving campaigns");
        }
    }

    /// <summary>
    /// Refresh campaign statistics
    /// </summary>
    /// <param name="id">Campaign ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id:int}/refresh-stats")]
    public async Task<ActionResult> RefreshCampaignStats(int id)
    {
        try
        {
            await _campaignService.RefreshCampaignStatsAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stats for campaign {CampaignId}", id);
            return StatusCode(500, "An error occurred while refreshing campaign statistics");
        }
    }
}
