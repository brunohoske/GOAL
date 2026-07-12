using Goal.Domain.Common;

namespace Goal.Domain.Identity;

/// <summary>Rotating refresh token (stored hashed). Rotation chains via ReplacedByTokenId.</summary>
public class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
