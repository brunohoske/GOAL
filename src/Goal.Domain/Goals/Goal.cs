using Goal.Domain.Common;
using Goal.Domain.Identity;
using Goal.Domain.Sprints;

namespace Goal.Domain.Goals;

/// <summary>
/// A collective objective (e.g. "Learn to build a game"). Aggregate root.
/// Most behavioural parameters live in the 1:1 immutable <see cref="GoalSettings"/>.
/// </summary>
public class GoalAggregate : Entity
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    public Guid AdminUserId { get; set; }
    public User? Admin { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    /// <summary>Convenience cache of the currently active sprint.</summary>
    public Guid? CurrentSprintId { get; set; }

    /// <summary>IANA timezone (e.g. "America/Sao_Paulo") — drives sprint close &amp; quiet hours.</summary>
    public string TimeZone { get; set; } = "America/Sao_Paulo";

    public GoalSettings Settings { get; set; } = default!;
    public ICollection<GoalMember> Members { get; set; } = new List<GoalMember>();
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
}
