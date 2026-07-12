using Goal.Domain.Common;

namespace Goal.Domain.Identity;

/// <summary>An FCM registration token for a user's device. Used to deliver push notifications.</summary>
public class DeviceToken : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string FcmToken { get; set; } = default!;
    public DevicePlatform Platform { get; set; }
    public bool IsActive { get; set; } = true;          // invalidated when FCM returns UNREGISTERED
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
