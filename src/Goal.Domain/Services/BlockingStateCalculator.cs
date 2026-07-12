using Goal.Domain.Goals;
using Goal.Domain.Sprints;

namespace Goal.Domain.Services;

/// <summary>
/// Computes whether a member's social apps should be blocked, and how far they are from
/// unblocking. Pure and deterministic — the single source of the blocking rule.
///
/// Rule:
///   - Normally the member is blocked until they reach UnblockThresholdXp
///     (= ceil(EffectiveTargetXp * UnblockThresholdPct), e.g. 70%).
///   - When only FinalTriggerDaysBefore days (or fewer) remain, the target rises to
///     ceil(EffectiveTargetXp * FinalTriggerTargetPct) (e.g. 100%).
///   - Accumulated debt raises EffectiveTargetXp, making every threshold harder to reach.
/// </summary>
public static class BlockingStateCalculator
{
    public static BlockingState Calculate(
        SprintMemberState state,
        Sprint sprint,
        GoalSettings settings,
        DateTimeOffset now,
        TimeZoneInfo goalTimeZone)
    {
        var daysRemaining = DaysRemaining(sprint.EndAt, now, goalTimeZone);

        int targetXp;
        bool requiresFull;
        if (daysRemaining <= settings.FinalTriggerDaysBefore)
        {
            targetXp = (int)Math.Ceiling(state.EffectiveTargetXp * settings.FinalTriggerTargetPct);
            requiresFull = settings.FinalTriggerTargetPct >= 1.00m;
        }
        else
        {
            targetXp = state.UnblockThresholdXp;
            requiresFull = false;
        }

        return new BlockingState
        {
            EarnedXp = state.EarnedXp,
            EffectiveTargetXp = state.EffectiveTargetXp,
            TargetXp = targetXp,
            UnblockThresholdXp = state.UnblockThresholdXp,
            DebtXp = state.CarriedDebtXp,
            DaysRemaining = daysRemaining,
            RequiresFullCompletion = requiresFull,
            IsBlocked = state.EarnedXp < targetXp
        };
    }

    /// <summary>
    /// Whole calendar days remaining until sprint end, evaluated in the goal's timezone so
    /// "1 day before" lines up with the member's local day boundaries (DST-safe).
    /// </summary>
    private static int DaysRemaining(DateTimeOffset endAt, DateTimeOffset now, TimeZoneInfo tz)
    {
        if (now >= endAt) return 0;
        var endLocalDate = TimeZoneInfo.ConvertTime(endAt, tz).Date;
        var nowLocalDate = TimeZoneInfo.ConvertTime(now, tz).Date;
        return Math.Max(0, (endLocalDate - nowLocalDate).Days);
    }
}
