using System.ComponentModel.DataAnnotations;
using PaymentService.Models;

namespace PaymentService.DTOs;

public class ProcessPaymentRequest
{
    [Required]
    public int DonationId { get; set; }
    
    [Required]
    public int CampaignId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    [MaxLength(100)]
    public string DonorName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string DonorEmail { get; set; } = string.Empty;
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    [Required]
    public PaymentDetails PaymentDetails { get; set; } = new();
}

public class PaymentDetails
{
    // Credit/Debit Card
    [MaxLength(20)]
    public string? CardNumber { get; set; }
    
    [MaxLength(10)]
    public string? ExpiryDate { get; set; }
    
    [MaxLength(4)]
    public string? Cvv { get; set; }
    
    [MaxLength(100)]
    public string? CardHolderName { get; set; }
    
    // PayPal
    [MaxLength(200)]
    public string? PayPalEmail { get; set; }
    
    // Bank Transfer
    [MaxLength(50)]
    public string? AccountNumber { get; set; }
    
    [MaxLength(50)]
    public string? RoutingNumber { get; set; }
    
    // Digital Wallets
    [MaxLength(200)]
    public string? WalletId { get; set; }
    
    // Billing Address
    [MaxLength(200)]
    public string? BillingAddress { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
}
