using System.Text.Json;
using Goal.Application.Abstractions;
using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Notifications;

namespace Goal.Application.Notifications;

/// <summary>
/// Records an in-app Notification row and sends the matching push in one call, so every event
/// lands in the notification center even when FCM isn't configured or the push fails.
/// Callers own SaveChangesAsync — the row is committed inside their transaction.
/// </summary>
public sealed class Notifier
{
    private readonly IAppDbContext _db;
    private readonly IPushSender _push;

    public Notifier(IAppDbContext db, IPushSender push)
    {
        _db = db; _push = push;
    }

    public async Task NotifyAsync(GoalMember member, Guid? goalId, NotificationType type,
        string title, string body, IReadOnlyDictionary<string, string>? data = null, CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            GoalMemberId = member.Id,
            GoalId = goalId,
            Type = type,
            Title = title,
            Body = body,
            DataJson = data is null ? null : JsonSerializer.Serialize(data),
            Status = NotificationStatus.Sent
        });

        try
        {
            await _push.SendAsync(member.UserId, title, body, data, ct);
        }
        catch
        {
            // The in-app row is the durable record; push delivery is best-effort.
        }
    }
}
