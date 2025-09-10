using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;

/// <summary>
/// Represents a refresh token for JWT authentication
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The refresh token value
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User ID this token belongs to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the token was created
    /// </summary>
    [StringLength(45)]
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// When the token was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address from which the token was revoked
    /// </summary>
    [StringLength(45)]
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Whether the token is active (not expired and not revoked)
    /// </summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
