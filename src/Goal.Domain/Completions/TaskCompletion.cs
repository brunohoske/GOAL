using Goal.Domain.Common;
using Goal.Domain.Assignments;
using Goal.Domain.Goals;

namespace Goal.Domain.Completions;

/// <summary>
/// A member's submission to complete an assignment: the documentation (text always, plus
/// image/attachment/checklist per the snapshotted flags). Enters PendingReview and is
/// resolved by social voting (>= VoteApprovalThreshold of members) or at the deadline.
/// </summary>
public class TaskCompletion : Entity
{
    public Guid SprintTaskAssignmentId { get; set; }
    public SprintTaskAssignment? SprintTaskAssignment { get; set; }

    public Guid SubmittedByGoalMemberId { get; set; }
    public GoalMember? SubmittedByGoalMember { get; set; }

    public string TextContent { get; set; } = default!;   // required
    public CompletionStatus Status { get; set; } = CompletionStatus.PendingReview;

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ReviewDeadlineAt { get; set; }
    public bool DeliveredOnTime { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
    public int? AwardedXp { get; set; }      // frozen XP credited on approval
    public int Attempt { get; set; } = 1;

    /// <summary>Optimistic-concurrency token (PostgreSQL xmin) to guard against double-resolution.</summary>
    public uint Version { get; set; }

    public ICollection<CompletionAttachment> Attachments { get; set; } = new List<CompletionAttachment>();
    public ICollection<CompletionChecklistState> ChecklistStates { get; set; } = new List<CompletionChecklistState>();
    public ICollection<CompletionVote> Votes { get; set; } = new List<CompletionVote>();
}
