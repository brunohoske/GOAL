namespace Goal.Domain.Services;

public enum VotingOutcome { Pending, Approved, Rejected }

public readonly record struct VoteTallyResult
{
    public int EligibleVoters { get; init; }
    public int Approvals { get; init; }
    public int Rejections { get; init; }
    public decimal ApprovalThreshold { get; init; }
    public VotingOutcome Outcome { get; init; }

    public int ApprovalsNeeded => (int)Math.Ceiling(EligibleVoters * ApprovalThreshold);
    public int ApprovalsRemaining => Math.Max(0, ApprovalsNeeded - Approvals);
    public decimal ApprovalRatio => EligibleVoters <= 0 ? 0m : (decimal)Approvals / EligibleVoters;
}

/// <summary>
/// Tallies social approval. Denominator is the number of *eligible* voters (active members
/// excluding the author), NOT only those who voted — so a completion can't be approved with
/// one vote out of many. Resolves early when the outcome is mathematically decided.
/// </summary>
public static class VoteTally
{
    public static VoteTallyResult Tally(int eligibleVoters, int approvals, int rejections, decimal approvalThreshold)
    {
        var approvalsNeeded = (int)Math.Ceiling(eligibleVoters * approvalThreshold);

        VotingOutcome outcome;
        if (eligibleVoters <= 0)
        {
            // No one else can vote (e.g. solo goal) — auto-approve so the member isn't stuck.
            outcome = VotingOutcome.Approved;
        }
        else if (approvals >= approvalsNeeded)
        {
            outcome = VotingOutcome.Approved;
        }
        else if (eligibleVoters - rejections < approvalsNeeded)
        {
            // Even if every remaining voter approved, the threshold can't be met.
            outcome = VotingOutcome.Rejected;
        }
        else
        {
            outcome = VotingOutcome.Pending;
        }

        return new VoteTallyResult
        {
            EligibleVoters = eligibleVoters,
            Approvals = approvals,
            Rejections = rejections,
            ApprovalThreshold = approvalThreshold,
            Outcome = outcome
        };
    }
}
