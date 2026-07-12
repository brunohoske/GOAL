using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Goals;

/// <summary>
/// Admin-only. Archives the goal (soft delete): it vanishes from everyone's lists, the join
/// code stops working and blocking reports "free" — members are released automatically.
/// History (XP ledger, completions) is preserved for audit.
/// </summary>
public record DeleteGoalCommand(Guid GoalId) : IRequest<Result>;

public class DeleteGoalHandler : IRequestHandler<DeleteGoalCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteGoalHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteGoalCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var actor = await GoalAccess.FindMemberAsync(_db, cmd.GoalId, userId, ct);
        if (actor is null || actor.Role != MemberRole.Admin)
            return Error.Forbidden("Somente o admin pode excluir o GOAL.");

        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == cmd.GoalId, ct);
        if (goal is null) return Error.NotFound("GOAL não encontrado.");

        goal.Status = GoalStatus.Archived;

        // Deactivate pending nag schedules so archived goals stop notifying members.
        var memberIds = await _db.GoalMembers
            .Where(m => m.GoalId == cmd.GoalId).Select(m => m.Id).ToListAsync(ct);
        var schedules = await _db.NotificationSchedules
            .Where(n => memberIds.Contains(n.GoalMemberId) && n.IsActive).ToListAsync(ct);
        foreach (var s in schedules) s.IsActive = false;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
