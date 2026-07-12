using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Sprints;
using Goal.Domain.Tasks;

namespace Goal.Domain.Assignments;

/// <summary>
/// An instance of a TaskDefinition within a specific sprint, optionally assigned to a member.
/// XP/flags are snapshotted here so editing the TaskDefinition later doesn't rewrite history.
/// </summary>
public class SprintTaskAssignment : Entity
{
    public Guid SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public Guid TaskDefinitionId { get; set; }
    public TaskDefinition? TaskDefinition { get; set; }

    public Guid? AssignedToGoalMemberId { get; set; }   // null = unassigned pool/backlog
    public GoalMember? AssignedToGoalMember { get; set; }

    public AssignmentType AssignmentType { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Open;
    public DateTimeOffset? DueAt { get; set; }          // used for the on-time bonus

    public bool IsBacklog { get; set; }                 // carried from a previous sprint
    public Guid? OriginSprintId { get; set; }

    // --- Snapshot of the task's XP/flags at assignment time (historical integrity) ---
    public XpMode SnapshotXpMode { get; set; }
    public int? SnapshotManualXp { get; set; }
    public Difficulty? SnapshotDifficulty { get; set; }
    public int? SnapshotOnTimeBonusXp { get; set; }
    public int? SnapshotStreakBonusXp { get; set; }
    public bool SnapshotRequiresText { get; set; }
    public bool SnapshotRequiresImage { get; set; }
    public bool SnapshotRequiresAttachment { get; set; }
    public bool SnapshotHasChecklist { get; set; }
}
