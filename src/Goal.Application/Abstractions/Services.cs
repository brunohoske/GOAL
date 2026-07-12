using Goal.Domain.Identity;

namespace Goal.Application.Abstractions;

/// <summary>Issues and validates JWT access/refresh tokens.</summary>
public interface ITokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
    (string Token, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken();
    string HashRefreshToken(string token);
}

/// <summary>Hashes and verifies user passwords.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Sends push notifications to a user's devices via FCM.</summary>
public interface IPushSender
{
    Task SendAsync(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null, CancellationToken ct = default);
}

/// <summary>Stores uploaded files (completion images/attachments) and returns their URL.</summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}

/// <summary>Schedules background jobs (e.g. resolving a completion's vote at its deadline).</summary>
public interface IJobScheduler
{
    void ScheduleCompletionDeadline(Guid completionId, DateTimeOffset runAt);
}
