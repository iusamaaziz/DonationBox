using System.ComponentModel.DataAnnotations;

namespace DonorService.DTOs;

/// <summary>
/// DTO for donor information
/// </summary>
public class DonorDto
{
    /// <summary>
    /// Unique identifier matching AuthService User ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Donor biography
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Areas of interest for donations
    /// </summary>
    public List<string> Interests { get; set; } = new List<string>();

    /// <summary>
    /// Phone number for contact
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Whether the donor profile is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the donor profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the donor profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
