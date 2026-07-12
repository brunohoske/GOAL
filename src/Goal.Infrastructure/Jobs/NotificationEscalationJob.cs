using Goal.Application.Abstractions;
using Goal.Application.Notifications;
using Goal.Domain.Common;
using Goal.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Goal.Infrastructure.Jobs;

/// <summary>
/// Recurring: fires due "nagging" notifications and reschedules them. The more XP a member is
/// short and the less time remains, the shorter the interval (more frequent nudges). Stops when
/// the member is unblocked or the sprint closes.
/// </summary>
public sealed class NotificationEscalationJob
{
    private const int MinIntervalMinutes = 30;
    private const int MaxIntervalMinutes = 1440;
    private const int BaseIntervalMinutes = 720;

    private readonly IAppDbContext _db;
    private readonly Notifier _notifier;
    private readonly IClock _clock;

    public NotificationEscalationJob(IAppDbContext db, Notifier notifier, IClock clock)
    {
        _db = db; _notifier = notifier; _clock = clock;
    }

    public async Task RunAsync()
    {
        var now = _clock.UtcNow;
        var due = await _db.NotificationSchedules
            .Where(n => n.IsActive && n.NextFireAt <= now)
            .ToListAsync();

        foreach (var schedule in due)
        {
            var member = await _db.GoalMembers.FirstOrDefaultAsync(m => m.Id == schedule.GoalMemberId);
            var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == schedule.SprintId);
            var state = await _db.SprintMemberStates
                .FirstOrDefaultAsync(ms => ms.SprintId == schedule.SprintId && ms.GoalMemberId == schedule.GoalMemberId);
            if (member is null || sprint is null || state is null) { schedule.IsActive = false; continue; }

            var goal = await _db.Goals.Include(g => g.Settings).FirstAsync(g => g.Id == sprint.GoalId);
            var tz = ResolveTz(goal.TimeZone);
            var bs = BlockingStateCalculator.Calculate(state, sprint, goal.Settings, now, tz);

            if (!bs.IsBlocked) { schedule.IsActive = false; continue; }

            await _notifier.NotifyAsync(member, goal.Id, NotificationType.BlockedReminder,
                "Você está devendo XP 👀",
                $"Faltam {bs.XpRemainingToUnblock} XP para liberar seus apps nesta sprint.",
                new Dictionary<string, string> { ["goalId"] = goal.Id.ToString(), ["type"] = "BlockedReminder" });

            schedule.IntervalMinutes = NextInterval(bs);
            schedule.NextFireAt = now.AddMinutes(schedule.IntervalMinutes);
        }

        await _db.SaveChangesAsync();
    }

    private static int NextInterval(BlockingState bs)
    {
        // urgency grows with the XP gap fraction and shrinks with days remaining.
        var gapFraction = bs.EffectiveTargetXp <= 0 ? 0d : (double)bs.XpRemainingToUnblock / bs.EffectiveTargetXp;
        var daysFactor = Math.Max(1, bs.DaysRemaining);
        var urgency = Math.Max(0.1, gapFraction) * (3.0 / daysFactor);
        var interval = (int)(BaseIntervalMinutes / Math.Max(0.1, urgency));
        return Math.Clamp(interval, MinIntervalMinutes, MaxIntervalMinutes);
    }

    private static TimeZoneInfo ResolveTz(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
