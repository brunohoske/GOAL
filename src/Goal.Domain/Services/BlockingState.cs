namespace Goal.Domain.Services;

/// <summary>
/// The derived blocking state for a member in a sprint. Computed on demand (never persisted)
/// so it can never drift from the underlying XP/target values.
/// </summary>
public readonly record struct BlockingState
{
    public bool IsBlocked { get; init; }
    public int EarnedXp { get; init; }
    public int EffectiveTargetXp { get; init; }
    public int TargetXp { get; init; }              // the XP needed right now to be unblocked
    public int UnblockThresholdXp { get; init; }    // the normal (non-final) threshold
    public int DebtXp { get; init; }
    public int DaysRemaining { get; init; }
    public bool RequiresFullCompletion { get; init; }

    /// <summary>0..1 progress toward the effective target (capped at 1).</summary>
    public decimal CurrentPct =>
        EffectiveTargetXp <= 0 ? 1m : Math.Min(1m, (decimal)EarnedXp / EffectiveTargetXp);

    /// <summary>0..1 fraction of the effective target that the current target represents.</summary>
    public decimal TargetPct =>
        EffectiveTargetXp <= 0 ? 1m : Math.Min(1m, (decimal)TargetXp / EffectiveTargetXp);

    /// <summary>XP still needed to become unblocked (0 if already unblocked).</summary>
    public int XpRemainingToUnblock => Math.Max(0, TargetXp - EarnedXp);
}
