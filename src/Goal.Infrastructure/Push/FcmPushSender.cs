using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Goal.Application.Abstractions;
using Goal.Domain.Identity;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Goal.Infrastructure.Push;

/// <summary>
/// Sends FCM pushes to all active device tokens of a user. Tokens FCM reports as
/// unregistered/invalid are deactivated so we stop pushing to dead devices.
/// No-op (logs a warning) when Firebase credentials are not configured, so local dev still runs.
/// </summary>
public sealed class FcmPushSender : IPushSender
{
    private readonly IAppDbContext _db;
    private readonly ILogger<FcmPushSender> _logger;
    private readonly bool _enabled;

    public FcmPushSender(IAppDbContext db, ILogger<FcmPushSender> logger)
    {
        _db = db;
        _logger = logger;
        _enabled = FirebaseApp.DefaultInstance is not null;
    }

    public async Task SendAsync(Guid userId, string title, string body,
        IReadOnlyDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var tokens = await _db.DeviceTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync(ct);

        if (tokens.Count == 0) return;

        if (!_enabled)
        {
            _logger.LogWarning("FCM not configured; skipping push to {UserId}: {Title}", userId, title);
            return;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens.Select(t => t.FcmToken).ToList(),
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data = data?.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, ct);
        if (response.FailureCount > 0)
            DeactivateInvalidTokens(tokens, response);
    }

    private void DeactivateInvalidTokens(List<DeviceToken> tokens, BatchResponse response)
    {
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (r.IsSuccess) continue;
            var code = r.Exception?.MessagingErrorCode;
            if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                tokens[i].IsActive = false;
        }
        _db.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
