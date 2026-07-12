using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
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

/// <summary>Admin-only: adds a task to the goal's catalog (with optional checklist subtasks).</summary>
public class CreateTaskDefinitionHandler : IRequestHandler<CreateTaskDefinitionCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateTaskDefinitionHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateTaskDefinitionCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var member = await GoalAccess.FindMemberAsync(_db, cmd.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");
        if (member.Role != MemberRole.Admin) return Error.Forbidden("Only the admin can create tasks.");

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
            CreatedByUserId = userId
        };
        if (cmd.HasChecklist)
            foreach (var item in cmd.ChecklistItems)
                task.ChecklistItems.Add(new ChecklistItemTemplate
                { Label = item.Label, OrderIndex = item.OrderIndex, IsRequired = item.IsRequired });

        _db.TaskDefinitions.Add(task);
        await _db.SaveChangesAsync(ct);
        return Result.Success(task.Id);
    }
}
