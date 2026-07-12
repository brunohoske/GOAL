using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Sprints;

namespace Goal.Domain.Notifications;

/// <summary>
/// Drives the escalating "nagging" notifications. The escalation job recomputes
/// IntervalMinutes from the member's XP gap / days remaining and reschedules NextFireAt.
/// Deactivated when the member clears their target or the sprint closes.
/// </summary>
public class NotificationSchedule : Entity
{
    public Guid GoalMemberId { get; set; }
    public GoalMember? GoalMember { get; set; }

    public Guid SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public NotificationScheduleKind Kind { get; set; }
    public DateTimeOffset NextFireAt { get; set; }
    public int IntervalMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
