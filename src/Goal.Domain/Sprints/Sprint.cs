using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Sprints;

public class Sprint : Entity
{
    public Guid GoalId { get; set; }
    public GoalAggregate? Goal { get; set; }

    public int SequenceNumber { get; set; }          // 1, 2, 3...
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Active;
    public DateTimeOffset? ClosedAt { get; set; }

    public ICollection<SprintMemberState> MemberStates { get; set; } = new List<SprintMemberState>();
}
