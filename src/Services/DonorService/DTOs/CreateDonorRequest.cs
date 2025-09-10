using System.ComponentModel.DataAnnotations;

namespace DonorService.DTOs;

/// <summary>
/// Request DTO for creating or updating donor profile
/// </summary>
public class CreateDonorRequest
{
    /// <summary>
    /// Unique identifier matching AuthService User ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Donor biography
    /// </summary>
    [StringLength(1000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Areas of interest for donations
    /// </summary>
    public List<string> Interests { get; set; } = new List<string>();

    /// <summary>
    /// Phone number for contact
    /// </summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    [StringLength(500)]
    public string? Address { get; set; }
}
