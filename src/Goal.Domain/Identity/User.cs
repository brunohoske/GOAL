using Goal.Domain.Common;

namespace Goal.Domain.Identity;

public class User : Entity
{
    public string Email { get; set; } = default!;        // citext (case-insensitive), unique
    public string DisplayName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? AvatarUrl { get; set; }

    public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
