using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Sprints;

/// <summary>
/// Append-only ledger of XP credits — the source of truth for a member's earned XP.
/// Debt is NOT modelled here (it lives on SprintMemberState as Carried/EndDebtXp); this keeps
/// "XP earned" cleanly separated from "target to hit".
/// </summary>
public class XpLedgerEntry : Entity
{
    public Guid GoalMemberId { get; set; }
    public GoalMember? GoalMember { get; set; }

    public Guid SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public XpSourceType SourceType { get; set; }
    public Guid? SourceCompletionId { get; set; }

    public int Amount { get; set; }       // always positive (a credit)
    public string? Reason { get; set; }
}
