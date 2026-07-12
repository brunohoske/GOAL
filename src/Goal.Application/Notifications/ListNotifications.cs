using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Notifications;

public record NotificationDto(
    Guid Id,
    Guid? GoalId,
    NotificationType Type,
    string Title,
    string Body,
    string? DataJson,
    bool IsRead,
    DateTimeOffset CreatedAt);

/// <summary>Lists the current user's notifications across all goals, newest first.</summary>
public record ListNotificationsQuery : IRequest<Result<IReadOnlyList<NotificationDto>>>;

public class ListNotificationsHandler : IRequestHandler<ListNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    private const int PageSize = 100;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListNotificationsHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(ListNotificationsQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var items = await _db.Notifications
            .Join(_db.GoalMembers.Where(m => m.UserId == userId),
                n => n.GoalMemberId, m => m.Id, (n, _) => n)
            .OrderByDescending(n => n.CreatedAt)
            .Take(PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.GoalId, n.Type, n.Title, n.Body, n.DataJson,
                n.Status == NotificationStatus.Read, n.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<NotificationDto>>(items);
    }
}
