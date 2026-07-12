using Goal.Domain.Common;

namespace Goal.Domain.Tasks;

/// <summary>A subtask defined by the admin for a task that HasChecklist.</summary>
public class ChecklistItemTemplate : Entity
{
    public Guid TaskDefinitionId { get; set; }
    public TaskDefinition? TaskDefinition { get; set; }

    public string Label { get; set; } = default!;
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; } = true;
}
