using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Tasks;

public record ChecklistItemDto(Guid Id, string Label, bool IsRequired, int OrderIndex);

public record TaskDefinitionDto(
    Guid Id,
    string Title,
    string? Description,
    XpMode XpMode,
    int? ManualXp,
    Difficulty? Difficulty,
    bool RequiresText,
    bool RequiresImage,
    bool RequiresAttachment,
    bool HasChecklist,
    int EstimatedXp,
    IReadOnlyList<ChecklistItemDto> ChecklistItems,
    TaskApprovalStatus ApprovalStatus,
    string? ProposedByName,
    bool ProposedByMe);

public record ListTasksQuery(Guid GoalId) : IRequest<Result<IReadOnlyList<TaskDefinitionDto>>>;

public class ListTasksHandler : IRequestHandler<ListTasksQuery, Result<IReadOnlyList<TaskDefinitionDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListTasksHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TaskDefinitionDto>>> Handle(ListTasksQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var member = await GoalAccess.FindMemberAsync(_db, q.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");

        var settings = await _db.GoalSettings.FirstAsync(s => s.GoalId == q.GoalId, ct);

        // Rejected proposals have IsActive = false, so they drop out naturally.
        var tasks = await _db.TaskDefinitions
            .Where(t => t.GoalId == q.GoalId && t.IsActive)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        // Resolve proposer names for pending tasks so the admin knows who asked.
        var proposerIds = tasks.Where(t => t.ApprovalStatus == TaskApprovalStatus.Pending)
            .Select(t => t.CreatedByUserId).Distinct().ToList();
        var proposerNames = await _db.Users
            .Where(u => proposerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var taskIds = tasks.Select(t => t.Id).ToList();
        var checklists = (await _db.ChecklistItemTemplates
                .Where(c => taskIds.Contains(c.TaskDefinitionId))
                .OrderBy(c => c.OrderIndex)
                .ToListAsync(ct))
            .GroupBy(c => c.TaskDefinitionId)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyList<ChecklistItemDto>)g.Select(c => new ChecklistItemDto(c.Id, c.Label, c.IsRequired, c.OrderIndex)).ToList());

        var result = tasks.Select(t => new TaskDefinitionDto(
            t.Id, t.Title, t.Description, t.XpMode, t.ManualXp, t.Difficulty,
            t.RequiresText, t.RequiresImage, t.RequiresAttachment, t.HasChecklist,
            EstimateXp(t, settings),
            checklists.TryGetValue(t.Id, out var items) ? items : Array.Empty<ChecklistItemDto>(),
            t.ApprovalStatus,
            proposerNames.TryGetValue(t.CreatedByUserId, out var name) ? name : null,
            t.CreatedByUserId == userId)).ToList();

        return Result.Success<IReadOnlyList<TaskDefinitionDto>>(result);
    }

    private static int EstimateXp(Domain.Tasks.TaskDefinition t, Domain.Goals.GoalSettings s) => t.XpMode switch
    {
        XpMode.Manual => t.ManualXp ?? 0,
        XpMode.Scalable => s.XpBaseFor(t.Difficulty ?? Difficulty.Easy),
        _ => 0
    };
}
