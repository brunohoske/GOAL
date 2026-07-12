using Goal.Domain.Assignments;
using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Services;

/// <summary>Breakdown of an XP award, so each component can be written to the ledger separately.</summary>
public readonly record struct XpAward(int Base, int OnTimeBonus, int StreakBonus)
{
    public int Total => Base + OnTimeBonus + StreakBonus;
}

/// <summary>
/// Computes the XP an approved completion is worth, from the assignment's *snapshotted* values
/// (so later edits to the TaskDefinition can't rewrite history). Called at approval time.
/// </summary>
public static class XpCalculator
{
    public static XpAward Calculate(SprintTaskAssignment assignment, GoalSettings settings, bool deliveredOnTime, bool qualifiesForStreak)
    {
        var baseXp = assignment.SnapshotXpMode switch
        {
            XpMode.Manual => assignment.SnapshotManualXp ?? 0,
            XpMode.Scalable => settings.XpBaseFor(assignment.SnapshotDifficulty ?? Difficulty.Easy),
            _ => 0
        };

        var onTime = deliveredOnTime ? assignment.SnapshotOnTimeBonusXp ?? 0 : 0;
        var streak = qualifiesForStreak ? assignment.SnapshotStreakBonusXp ?? 0 : 0;

        return new XpAward(baseXp, onTime, streak);
    }
}
