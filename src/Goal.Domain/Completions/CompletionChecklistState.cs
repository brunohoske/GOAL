using Goal.Domain.Common;
using Goal.Domain.Tasks;

namespace Goal.Domain.Completions;

/// <summary>The checked/unchecked state of a checklist item within a specific completion.</summary>
public class CompletionChecklistState : Entity
{
    public Guid TaskCompletionId { get; set; }
    public TaskCompletion? TaskCompletion { get; set; }

    public Guid ChecklistItemTemplateId { get; set; }
    public ChecklistItemTemplate? ChecklistItemTemplate { get; set; }

    public bool IsChecked { get; set; }
}
