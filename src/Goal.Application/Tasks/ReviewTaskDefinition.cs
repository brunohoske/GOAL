using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Application.Notifications;
using Goal.Domain.Common;
using Goal.Domain.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Tasks;

/// <summary>
/// Admin resolves a member-proposed task: approve (optionally adjusting ANY field first —
/// e.g. bump 20 XP to 40) or reject. The proposer is notified either way. All non-null
/// fields overwrite the proposal; a non-null ChecklistItems list replaces the checklist.
/// </summary>
public record ReviewTaskDefinitionCommand(
    Guid GoalId,
    Guid TaskDefinitionId,
    bool Approve,
    string? Title,
    string? Description,
    XpMode? XpMode,
    int? ManualXp,
    Difficulty? Difficulty,
    int? OnTimeBonusXp,
    int? StreakBonusXp,
    bool? RequiresImage,
    bool? RequiresAttachment,
    bool? HasChecklist,
    IReadOnlyList<ChecklistItemInput>? ChecklistItems) : IRequest<Result<Guid>>;

public class ReviewTaskDefinitionValidator : AbstractValidator<ReviewTaskDefinitionCommand>
{
    public ReviewTaskDefinitionValidator()
    {
        RuleFor(x => x.Title).MaximumLength(160);
        RuleFor(x => x.ManualXp).GreaterThan(0).When(x => x.ManualXp is not null);
    }
}

public class ReviewTaskDefinitionHandler : IRequestHandler<ReviewTaskDefinitionCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly Notifier _notifier;

    public ReviewTaskDefinitionHandler(IAppDbContext db, ICurrentUser currentUser, Notifier notifier)
    {
        _db = db; _currentUser = currentUser; _notifier = notifier;
    }

    public async Task<Result<Guid>> Handle(ReviewTaskDefinitionCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var member = await GoalAccess.FindMemberAsync(_db, cmd.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");
        if (member.Role != MemberRole.Admin)
            return Error.Forbidden("Only the admin can review proposed tasks.");

        var task = await _db.TaskDefinitions
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskDefinitionId && t.GoalId == cmd.GoalId, ct);
        if (task is null) return Error.NotFound("Task not found in this goal.");
        if (task.ApprovalStatus != TaskApprovalStatus.Pending)
            return Error.Validation("This task is not awaiting review.");

        if (cmd.Approve)
        {
            // Apply the admin's adjustments (any field), then publish.
            if (cmd.Title is not null) task.Title = cmd.Title;
            if (cmd.Description is not null) task.Description = cmd.Description;
            if (cmd.XpMode is not null) task.XpMode = cmd.XpMode.Value;
            if (cmd.ManualXp is not null) task.ManualXp = cmd.ManualXp;
            if (cmd.Difficulty is not null) task.Difficulty = cmd.Difficulty;
            if (cmd.OnTimeBonusXp is not null) task.OnTimeBonusXp = cmd.OnTimeBonusXp;
            if (cmd.StreakBonusXp is not null) task.StreakBonusXp = cmd.StreakBonusXp;
            if (cmd.RequiresImage is not null) task.RequiresImage = cmd.RequiresImage.Value;
            if (cmd.RequiresAttachment is not null) task.RequiresAttachment = cmd.RequiresAttachment.Value;
            if (cmd.HasChecklist is not null) task.HasChecklist = cmd.HasChecklist.Value;
            if (cmd.ChecklistItems is not null)
            {
                task.ChecklistItems.Clear();
                foreach (var item in cmd.ChecklistItems)
                    task.ChecklistItems.Add(new ChecklistItemTemplate
                    { Label = item.Label, OrderIndex = item.OrderIndex, IsRequired = item.IsRequired });
            }

            // Guard the XP invariants after edits.
            if (task.XpMode == XpMode.Manual && (task.ManualXp is null or <= 0))
                return Error.Validation("Manual XP must be positive.");
            if (task.XpMode == XpMode.Scalable && task.Difficulty is null)
                return Error.Validation("Difficulty is required for scalable tasks.");

            task.ApprovalStatus = TaskApprovalStatus.Approved;
        }
        else
        {
            // Rejected proposals leave the catalog entirely.
            task.ApprovalStatus = TaskApprovalStatus.Rejected;
            task.IsActive = false;
        }

        var proposer = await _db.GoalMembers.FirstOrDefaultAsync(
            m => m.GoalId == cmd.GoalId && m.UserId == task.CreatedByUserId && m.Status == MemberStatus.Active, ct);
        if (proposer is not null && proposer.UserId != userId)
        {
            await _notifier.NotifyAsync(proposer, cmd.GoalId,
                cmd.Approve ? NotificationType.TaskApproved : NotificationType.TaskRejected,
                cmd.Approve ? "Tarefa aprovada! 🎉" : "Tarefa recusada",
                cmd.Approve
                    ? $"Sua tarefa \"{task.Title}\" foi aprovada pelo admin e já está no catálogo."
                    : $"Sua tarefa \"{task.Title}\" foi recusada pelo admin.",
                new Dictionary<string, string> { ["goalId"] = cmd.GoalId.ToString(), ["taskId"] = task.Id.ToString() },
                ct);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(task.Id);
    }
}
