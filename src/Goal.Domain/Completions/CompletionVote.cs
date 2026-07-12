using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Completions;

/// <summary>One member's vote on a completion. The author does not vote on their own work.</summary>
public class CompletionVote : Entity
{
    public Guid TaskCompletionId { get; set; }
    public TaskCompletion? TaskCompletion { get; set; }

    public Guid VoterGoalMemberId { get; set; }
    public GoalMember? VoterGoalMember { get; set; }

    public VoteDecision Decision { get; set; }
    public string? Comment { get; set; }
}
