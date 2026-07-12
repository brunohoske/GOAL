using Goal.Domain.Common;

namespace Goal.Domain.Goals;

/// <summary>
/// Immutable-after-creation configuration of a Goal. This is where almost all of the
/// product's behaviour is parameterised so nothing is hard-coded. Same values apply to
/// every member of the Goal. Defined entirely in the "create goal" wizard.
/// </summary>
public class GoalSettings : Entity
{
    public Guid GoalId { get; set; }
    public GoalAggregate? Goal { get; set; }

    // --- Sprint / target ---
    public int SprintDurationDays { get; set; }
    public int BaseXpTargetPerSprint { get; set; }

    // --- Unblock threshold (e.g. 0.70 = unblock apps at 70% of the effective target) ---
    public decimal UnblockThresholdPct { get; set; } = 0.70m;

    // --- Final critical trigger: when N days remain, raise the target to FinalTriggerTargetPct ---
    public int FinalTriggerDaysBefore { get; set; } = 1;
    public decimal FinalTriggerTargetPct { get; set; } = 1.00m;

    // --- Social approval ---
    public decimal VoteApprovalThreshold { get; set; } = 0.60m;

    // --- Debt ---
    public bool DebtCarryEnabled { get; set; } = true;

    // --- Scalable XP table (base XP per difficulty for XpMode.Scalable tasks) ---
    public int XpScalableEasy { get; set; } = 10;
    public int XpScalableMedium { get; set; } = 25;
    public int XpScalableHard { get; set; } = 50;

    // --- "Chaos mode" nags. Unlike the rest of GoalSettings, these CAN be toggled off later
    //     (admin edit). They only fire while the member is blocked AND within their DaysBefore window. ---

    /// <summary>Random full-screen overlay nag: ~35% chance every ~30 min.</summary>
    public bool RandomOverlayEnabled { get; set; } = false;
    public int RandomOverlayDaysBefore { get; set; } = 3;

    /// <summary>Typing sabotage: ~25% chance per keystroke to replace the field with CustomText.</summary>
    public bool TypingSabotageEnabled { get; set; } = false;
    public int TypingSabotageDaysBefore { get; set; } = 3;

    /// <summary>Replacement text. Supports {xp} (remaining XP) and {nome} (member display name).</summary>
    public string? TypingSabotageText { get; set; }

    /// <summary>Apps blocked while the member is below target (chosen at creation).</summary>
    public ICollection<GoalBlockedApp> BlockedApps { get; set; } = new List<GoalBlockedApp>();

    public int XpBaseFor(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => XpScalableEasy,
        Difficulty.Medium => XpScalableMedium,
        Difficulty.Hard => XpScalableHard,
        _ => XpScalableEasy
    };
}
