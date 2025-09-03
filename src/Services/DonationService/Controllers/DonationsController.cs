using Microsoft.AspNetCore.Mvc;
using DonationService.DTOs;
using DonationService.Models;
using DonationService.Services;

namespace DonationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DonationsController : ControllerBase
{
    private readonly IDonationService _donationService;
    private readonly ILogger<DonationsController> _logger;

    public DonationsController(IDonationService donationService, ILogger<DonationsController> logger)
    {
        _donationService = donationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new donation pledge
    /// </summary>
    /// <param name="request">Donation creation request</param>
    /// <returns>Created donation</returns>
    [HttpPost]
    public async Task<ActionResult<DonationResponse>> CreateDonation([FromBody] CreateDonationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var donation = await _donationService.CreateDonationAsync(request);
            var response = MapToResponse(donation);
            return CreatedAtAction(nameof(GetDonation), new { id = donation.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid campaign ID {CampaignId} for donation", request.CampaignId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Campaign {CampaignId} not accepting donations", request.CampaignId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating donation for campaign {CampaignId}", request.CampaignId);
            return StatusCode(500, "An error occurred while creating the donation");
        }
    }

    /// <summary>
    /// Get a specific donation by ID
    /// </summary>
    /// <param name="id">Donation ID</param>
    /// <returns>Donation details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DonationResponse>> GetDonation(int id)
    {
        try
        {
            var donation = await _donationService.GetDonationByIdAsync(id);
            if (donation == null)
            {
                return NotFound($"Donation with ID {id} not found");
            }

            var response = MapToResponse(donation);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving donation {DonationId}", id);
            return StatusCode(500, "An error occurred while retrieving the donation");
        }
    }

    /// <summary>
    /// Get donation by transaction ID
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <returns>Donation details</returns>
    [HttpGet("transaction/{transactionId}")]
    public async Task<ActionResult<DonationResponse>> GetDonationByTransactionId(string transactionId)
    {
        try
        {
            var donation = await _donationService.GetDonationByTransactionIdAsync(transactionId);
            if (donation == null)
            {
                return NotFound($"Donation with transaction ID {transactionId} not found");
            }

            var response = MapToResponse(donation);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving donation by transaction ID {TransactionId}", transactionId);
            return StatusCode(500, "An error occurred while retrieving the donation");
        }
    }

    /// <summary>
    /// Get all donations for a specific campaign
    /// </summary>
    /// <param name="campaignId">Campaign ID</param>
    /// <returns>List of donations for the campaign</returns>
    [HttpGet("campaign/{campaignId:int}")]
    public async Task<ActionResult<IEnumerable<DonationResponse>>> GetDonationsByCampaign(int campaignId)
    {
        try
        {
            var donations = await _donationService.GetDonationsByCampaignAsync(campaignId);
            var responses = donations.Select(MapToResponse);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving donations for campaign {CampaignId}", campaignId);
            return StatusCode(500, "An error occurred while retrieving donations");
        }
    }

    /// <summary>
    /// Process a donation payment (webhook endpoint for payment processors)
    /// </summary>
    /// <param name="id">Donation ID</param>
    /// <param name="request">Payment processing request</param>
    /// <returns>Success status</returns>
    [HttpPost("{id:int}/process")]
    public async Task<ActionResult> ProcessDonation(int id, [FromBody] ProcessDonationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _donationService.ProcessDonationAsync(id, request.TransactionId, request.Status);
            if (!result)
            {
                return NotFound($"Donation with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing donation {DonationId}", id);
            return StatusCode(500, "An error occurred while processing the donation");
        }
    }

    private static DonationResponse MapToResponse(Donation donation)
    {
        return new DonationResponse
        {
            Id = donation.Id,
            CampaignId = donation.CampaignId,
            Amount = donation.Amount,
            DonorName = donation.DonorName,
            DonorEmail = donation.DonorEmail,
            Message = donation.Message,
            IsAnonymous = donation.IsAnonymous,
            TransactionId = donation.TransactionId,
            PaymentStatus = donation.PaymentStatus,
            CreatedAt = donation.CreatedAt,
            ProcessedAt = donation.ProcessedAt,
            Campaign = new CampaignSummary
            {
                Id = donation.Campaign.Id,
                Title = donation.Campaign.Title,
                Goal = donation.Campaign.Goal,
                CurrentAmount = donation.Campaign.CurrentAmount,
                Status = donation.Campaign.Status
            }
        };
    }
}

public class ProcessDonationRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
}
