using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Application.Notifications;
using Goal.Domain.Common;
using Goal.Domain.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Tasks;

public record ChecklistItemInput(string Label, int OrderIndex, bool IsRequired);

public record CreateTaskDefinitionCommand(
    Guid GoalId,
    string Title,
    string? Description,
    XpMode XpMode,
    int? ManualXp,
    Difficulty? Difficulty,
    int? OnTimeBonusXp,
    int? StreakBonusXp,
    bool RequiresImage,
    bool RequiresAttachment,
    bool HasChecklist,
    IReadOnlyList<ChecklistItemInput> ChecklistItems) : IRequest<Result<Guid>>;

public class CreateTaskDefinitionValidator : AbstractValidator<CreateTaskDefinitionCommand>
{
    public CreateTaskDefinitionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.ManualXp).NotNull().GreaterThan(0)
            .When(x => x.XpMode == XpMode.Manual)
            .WithMessage("Manual XP is required and must be positive for manual tasks.");
        RuleFor(x => x.Difficulty).NotNull()
            .When(x => x.XpMode == XpMode.Scalable)
            .WithMessage("Difficulty is required for scalable tasks.");
    }
}

/// <summary>
/// Adds a task to the goal's catalog (with optional checklist subtasks). Any member may
/// create: the admin's tasks go live immediately, a regular member's task enters the
/// Pending state and the admins are notified to review (and possibly adjust) it.
/// </summary>
public class CreateTaskDefinitionHandler : IRequestHandler<CreateTaskDefinitionCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly Notifier _notifier;

    public CreateTaskDefinitionHandler(IAppDbContext db, ICurrentUser currentUser, Notifier notifier)
    {
        _db = db; _currentUser = currentUser; _notifier = notifier;
    }

    public async Task<Result<Guid>> Handle(CreateTaskDefinitionCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var member = await GoalAccess.FindMemberAsync(_db, cmd.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");
        var isAdmin = member.Role == MemberRole.Admin;

        var task = new TaskDefinition
        {
            GoalId = cmd.GoalId,
            Title = cmd.Title,
            Description = cmd.Description,
            XpMode = cmd.XpMode,
            ManualXp = cmd.ManualXp,
            Difficulty = cmd.Difficulty,
            OnTimeBonusXp = cmd.OnTimeBonusXp,
            StreakBonusXp = cmd.StreakBonusXp,
            RequiresText = true,
            RequiresImage = cmd.RequiresImage,
            RequiresAttachment = cmd.RequiresAttachment,
            HasChecklist = cmd.HasChecklist,
            ApprovalStatus = isAdmin ? TaskApprovalStatus.Approved : TaskApprovalStatus.Pending,
            CreatedByUserId = userId
        };
        if (cmd.HasChecklist)
            foreach (var item in cmd.ChecklistItems)
                task.ChecklistItems.Add(new ChecklistItemTemplate
                { Label = item.Label, OrderIndex = item.OrderIndex, IsRequired = item.IsRequired });

        _db.TaskDefinitions.Add(task);

        if (!isAdmin)
        {
            var proposerName = await _db.Users.Where(u => u.Id == userId)
                .Select(u => u.DisplayName).FirstAsync(ct);
            var admins = await _db.GoalMembers
                .Where(m => m.GoalId == cmd.GoalId && m.Role == MemberRole.Admin && m.Status == MemberStatus.Active)
                .ToListAsync(ct);
            foreach (var admin in admins)
                await _notifier.NotifyAsync(admin, cmd.GoalId, NotificationType.TaskProposed,
                    "Nova tarefa para revisar",
                    $"{proposerName} sugeriu a tarefa \"{cmd.Title}\". Ajuste se precisar e aprove ou recuse.",
                    new Dictionary<string, string> { ["goalId"] = cmd.GoalId.ToString(), ["taskId"] = task.Id.ToString() },
                    ct);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(task.Id);
    }
}
