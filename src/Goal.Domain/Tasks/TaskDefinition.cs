using Goal.Domain.Common;
using Goal.Domain.Goals;

namespace Goal.Domain.Tasks;

/// <summary>
/// A task in a Goal's catalog. Admin-created tasks are immediately available; tasks
/// proposed by regular members wait as Pending until the admin approves them. It is a
/// *template* — the actual per-sprint instance a member works on is a SprintTaskAssignment.
/// </summary>
public class TaskDefinition : Entity
{
    public Guid GoalId { get; set; }
    public GoalAggregate? Goal { get; set; }

    public string Title { get; set; } = default!;
    public string? Description { get; set; }

    // --- XP ---
    public XpMode XpMode { get; set; }
    public int? ManualXp { get; set; }              // used when XpMode == Manual
    public Difficulty? Difficulty { get; set; }     // used when XpMode == Scalable
    public int? OnTimeBonusXp { get; set; }         // bonus when delivered on time
    public int? StreakBonusXp { get; set; }         // bonus when the member is on a streak

    // --- Documentation requirements for completion ---
    public bool RequiresText { get; set; } = true;  // INVARIANT: always required
    public bool RequiresImage { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool HasChecklist { get; set; }

    public bool IsActive { get; set; } = true;
    public TaskApprovalStatus ApprovalStatus { get; set; } = TaskApprovalStatus.Approved;
    public Guid CreatedByUserId { get; set; }

    public ICollection<ChecklistItemTemplate> ChecklistItems { get; set; } = new List<ChecklistItemTemplate>();
}
