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
    /// <remarks>
    /// **Authentication Required**: This endpoint requires a valid JWT token in the Authorization header.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/donations
    ///     {
    ///         "campaignId": 1,
    ///         "amount": 100.00,
    ///         "donorName": "John Doe",
    ///         "donorEmail": "john.doe@example.com",
    ///         "message": "Happy to support this cause!"
    ///     }
    /// 
    /// </remarks>
    [HttpPost]
    [Authorize]
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
            // Campaign details should be fetched from CampaignService when needed
            Campaign = null
        };
    }
}

public class ProcessDonationRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
}
