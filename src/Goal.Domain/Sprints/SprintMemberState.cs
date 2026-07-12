using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Sprints;

/// <summary>
/// Per-member state within a sprint. This is the central record the blocking calculation
/// reads from. One row per (Sprint x Member).
///
///   EffectiveTargetXp = BaseTargetXp + CarriedDebtXp
///   UnblockThresholdXp = ceil(EffectiveTargetXp * Goal.UnblockThresholdPct)
///
/// EarnedXp is a denormalised, incrementally-maintained sum of the XP ledger for fast reads;
/// a daily reconciliation job recomputes it from the ledger as source of truth.
/// </summary>
public class SprintMemberState : Entity
{
    public Guid SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public Guid GoalMemberId { get; set; }
    public GoalMember? GoalMember { get; set; }

    public int BaseTargetXp { get; set; }
    public int CarriedDebtXp { get; set; }
    public int EffectiveTargetXp { get; set; }
    public int EarnedXp { get; set; }
    public int UnblockThresholdXp { get; set; }

    // Filled at sprint close:
    public int? EndDebtXp { get; set; }
    public bool ReachedThreshold { get; set; }

    /// <summary>Recomputes derived target fields from the base/debt inputs and the goal's threshold.</summary>
    public void RecalculateTargets(decimal unblockThresholdPct)
    {
        EffectiveTargetXp = BaseTargetXp + CarriedDebtXp;
        UnblockThresholdXp = (int)Math.Ceiling(EffectiveTargetXp * unblockThresholdPct);
    }
}
