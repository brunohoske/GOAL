using Goal.Domain.Goals;
using Goal.Domain.Sprints;

namespace Goal.Domain.Services;

public readonly record struct SprintCloseOutcome(int EndDebtXp, bool ReachedThreshold);

/// <summary>
/// Computes sprint-close results and how a member's state carries into the next sprint.
///
///   endDebt = max(0, EffectiveTargetXp - EarnedXp)
///   next.CarriedDebt = endDebt        (if DebtCarryEnabled)
///   next.EffectiveTarget = BaseTarget + next.CarriedDebt
///
/// Excess XP above the target does NOT carry over — each sprint's earned counter resets;
/// only debt accumulates. This is intentional, to keep the pressure on.
/// </summary>
public static class SprintDebtCalculator
{
    public static SprintCloseOutcome Close(SprintMemberState state)
    {
        var endDebt = Math.Max(0, state.EffectiveTargetXp - state.EarnedXp);
        var reached = state.EarnedXp >= state.UnblockThresholdXp;
        return new SprintCloseOutcome(endDebt, reached);
    }

    /// <summary>Builds the next sprint's state for a member, carrying debt forward per settings.</summary>
    public static SprintMemberState CreateNextState(
        Guid nextSprintId,
        Guid goalMemberId,
        SprintCloseOutcome previousOutcome,
        GoalSettings settings)
    {
        var carriedDebt = settings.DebtCarryEnabled ? previousOutcome.EndDebtXp : 0;
        var next = new SprintMemberState
        {
            SprintId = nextSprintId,
            GoalMemberId = goalMemberId,
            BaseTargetXp = settings.BaseXpTargetPerSprint,
            CarriedDebtXp = carriedDebt,
            EarnedXp = 0
        };
        next.RecalculateTargets(settings.UnblockThresholdPct);
        return next;
    }
}
