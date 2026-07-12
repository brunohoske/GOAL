using Goal.Domain.Common;

namespace Goal.Domain.Goals;

public class GoalInvite : Entity
{
    public Guid GoalId { get; set; }
    public GoalAggregate? Goal { get; set; }

    public string InvitedEmail { get; set; } = default!;   // citext
    public string Token { get; set; } = default!;          // unique, shared in invite link
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public bool CanBeAccepted => Status == InviteStatus.Pending && DateTimeOffset.UtcNow < ExpiresAt;
}
