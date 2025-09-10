using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Models;

/// <summary>
/// Represents a user in the authentication system
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User's email address (unique)
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password (null for OAuth users)
    /// </summary>
    [StringLength(255)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Provider used for authentication (Local, Google)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Provider-specific user ID (for OAuth users)
    /// </summary>
    [StringLength(255)]
    public string? ProviderId { get; set; }

    /// <summary>
    /// User's profile picture URL
    /// </summary>
    [StringLength(500)]
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Whether the user's email has been verified
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Full name of the user
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
