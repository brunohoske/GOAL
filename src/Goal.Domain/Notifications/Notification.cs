using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Notifications;

public class Notification : Entity
{
    public Guid GoalMemberId { get; set; }
    public GoalMember? GoalMember { get; set; }

    public Guid? GoalId { get; set; }

    public NotificationType Type { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;

    /// <summary>JSON payload for deep-linking (goalId/sprintId/completionId).</summary>
    public string? DataJson { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTimeOffset? ReadAt { get; set; }
}
