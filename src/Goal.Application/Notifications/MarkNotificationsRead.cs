using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Notifications;

/// <summary>Marks one notification as read (must belong to the current user).</summary>
public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

/// <summary>Marks all of the current user's notifications as read.</summary>
public record MarkAllNotificationsReadCommand : IRequest<Result>;

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public MarkNotificationReadHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db; _currentUser = currentUser; _clock = clock;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var notification = await _db.Notifications
            .Join(_db.GoalMembers.Where(m => m.UserId == userId),
                n => n.GoalMemberId, m => m.Id, (n, _) => n)
            .FirstOrDefaultAsync(n => n.Id == cmd.NotificationId, ct);
        if (notification is null) return Error.NotFound("Notification not found.");

        notification.Status = NotificationStatus.Read;
        notification.ReadAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public MarkAllNotificationsReadHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db; _currentUser = currentUser; _clock = clock;
    }

    public async Task<Result> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var now = _clock.UtcNow;
        var unread = await _db.Notifications
            .Join(_db.GoalMembers.Where(m => m.UserId == userId),
                n => n.GoalMemberId, m => m.Id, (n, _) => n)
            .Where(n => n.Status != NotificationStatus.Read)
            .ToListAsync(ct);

        foreach (var n in unread)
        {
            n.Status = NotificationStatus.Read;
            n.ReadAt = now;
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
