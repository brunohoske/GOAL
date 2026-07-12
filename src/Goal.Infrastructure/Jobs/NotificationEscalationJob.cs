using Goal.Application.Abstractions;
using Goal.Application.Notifications;
using Goal.Domain.Common;
using Goal.Domain.Notifications;
using Goal.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Goal.Infrastructure.Jobs;

/// <summary>
/// Recurring: (1) starts a nag schedule for every blocked member who doesn't have one yet,
/// then (2) fires due nags and reschedules them. The more XP a member is short and the less
/// time remains, the shorter the interval (more frequent nudges). Stops when the member is
/// unblocked or the sprint closes.
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

        await BootstrapSchedulesAsync(now);

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

    /// <summary>
    /// Creates a nag schedule (firing immediately) for every member of an ACTIVE goal/sprint
    /// who is currently blocked and not being nagged yet. This is what starts the "flood".
    /// </summary>
    private async Task BootstrapSchedulesAsync(DateTimeOffset now)
    {
        var candidates = await _db.SprintMemberStates
            .Join(_db.Sprints.Where(s => s.Status == SprintStatus.Active),
                ms => ms.SprintId, s => s.Id, (ms, s) => new { ms, s })
            .Join(_db.Goals.Where(g => g.Status == GoalStatus.Active).Include(g => g.Settings),
                x => x.s.GoalId, g => g.Id, (x, g) => new { x.ms, x.s, g })
            .ToListAsync();
        if (candidates.Count == 0) return;

        var nagged = (await _db.NotificationSchedules
                .Where(n => n.IsActive)
                .Select(n => new { n.SprintId, n.GoalMemberId })
                .ToListAsync())
            .Select(n => (n.SprintId, n.GoalMemberId))
            .ToHashSet();

        var added = false;
        foreach (var c in candidates)
        {
            if (nagged.Contains((c.ms.SprintId, c.ms.GoalMemberId))) continue;

            var tz = ResolveTz(c.g.TimeZone);
            var bs = BlockingStateCalculator.Calculate(c.ms, c.s, c.g.Settings, now, tz);
            if (!bs.IsBlocked) continue;

            _db.NotificationSchedules.Add(new NotificationSchedule
            {
                GoalMemberId = c.ms.GoalMemberId,
                SprintId = c.ms.SprintId,
                Kind = NotificationScheduleKind.BlockedNudge,
                NextFireAt = now, // first nag goes out in this same run
                IntervalMinutes = NextInterval(bs),
                IsActive = true
            });
            added = true;
        }

        if (added) await _db.SaveChangesAsync();
    }

    private static int NextInterval(BlockingState bs)
    {
        // Urgency grows with the XP gap fraction and shrinks QUADRATICALLY as days run out,
        // so far from the deadline it's a daily reminder and the last day is a real flood.
        var gapFraction = bs.EffectiveTargetXp <= 0 ? 0d : (double)bs.XpRemainingToUnblock / bs.EffectiveTargetXp;
        var daysFactor = Math.Max(1, bs.DaysRemaining);
        var urgency = Math.Max(0.1, gapFraction) * Math.Pow(3.0 / daysFactor, 2);
        var interval = (int)(BaseIntervalMinutes / Math.Max(0.1, urgency));

        // Last day and still owing everything? Maximum pressure.
        if (bs.RequiresFullCompletion && gapFraction >= 0.5) interval = MinIntervalMinutes;

        return Math.Clamp(interval, MinIntervalMinutes, MaxIntervalMinutes);
    }

    private static TimeZoneInfo ResolveTz(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
