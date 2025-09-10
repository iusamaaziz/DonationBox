using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonorService.Models;

/// <summary>
/// Represents a donor profile in the system
/// </summary>
public class Donor
{
    /// <summary>
    /// Unique identifier matching AuthService User ID
    /// </summary>
    [Key]
    [Required]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Donor biography
    /// </summary>
    [StringLength(1000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Areas of interest for donations
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Interests { get; set; } // JSON array of interests

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

    /// <summary>
    /// Whether the donor profile is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the donor profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the donor profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for organizations created by this donor
    /// </summary>
    public ICollection<WelfareOrganization> Organizations { get; set; } = new List<WelfareOrganization>();

    /// <summary>
    /// Computed property to get interests as a list
    /// </summary>
    [NotMapped]
    public List<string> InterestsList
    {
        get
        {
            if (string.IsNullOrEmpty(Interests))
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Interests) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            Interests = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}
