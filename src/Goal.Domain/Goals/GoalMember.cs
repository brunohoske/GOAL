using Goal.Domain.Common;
using Goal.Domain.Identity;

namespace Goal.Domain.Goals;

/// <summary>Join of User and Goal carrying the member's role and status.</summary>
public class GoalMember : Entity
{
    public Guid GoalId { get; set; }
    public GoalAggregate? Goal { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public MemberRole Role { get; set; } = MemberRole.Member;
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive => Status == MemberStatus.Active;
}
