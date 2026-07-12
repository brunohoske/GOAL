using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Goals;

public record ListGoalsQuery : IRequest<Result<IReadOnlyList<GoalSummaryDto>>>;

public class ListGoalsHandler : IRequestHandler<ListGoalsQuery, Result<IReadOnlyList<GoalSummaryDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListGoalsHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<GoalSummaryDto>>> Handle(ListGoalsQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var goalIds = await _db.GoalMembers
            .Where(m => m.UserId == userId && m.Status == MemberStatus.Active)
            .Select(m => m.GoalId).ToListAsync(ct);

        var goals = await _db.Goals
            .Where(g => goalIds.Contains(g.Id) && g.Status == GoalStatus.Active)
            .Select(g => new
            {
                g.Id, g.Title, g.Description, g.AdminUserId, g.CurrentSprintId,
                MemberCount = g.Members.Count(m => m.Status == MemberStatus.Active)
            })
            .ToListAsync(ct);

        var sprintIds = goals.Where(g => g.CurrentSprintId != null).Select(g => g.CurrentSprintId!.Value).ToList();
        var sprints = await _db.Sprints
            .Where(s => sprintIds.Contains(s.Id))
            .Select(s => new { s.Id, s.SequenceNumber, s.EndAt })
            .ToDictionaryAsync(s => s.Id, ct);

        var result = goals.Select(g =>
        {
            sprints.TryGetValue(g.CurrentSprintId ?? Guid.Empty, out var sp);
            return new GoalSummaryDto(
                g.Id, g.Title, g.Description, g.AdminUserId == userId,
                sp?.SequenceNumber, sp?.EndAt, g.MemberCount);
        }).ToList();

        return Result.Success<IReadOnlyList<GoalSummaryDto>>(result);
    }
}

public record GetGoalQuery(Guid GoalId) : IRequest<Result<GoalDetailDto>>;

public class GetGoalHandler : IRequestHandler<GetGoalQuery, Result<GoalDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetGoalHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<GoalDetailDto>> Handle(GetGoalQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var member = await GoalAccess.FindMemberAsync(_db, q.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");

        var goal = await _db.Goals
            .Include(g => g.Settings).ThenInclude(s => s.BlockedApps)
            .FirstOrDefaultAsync(g => g.Id == q.GoalId, ct);
        if (goal is null) return Error.NotFound("Goal not found.");

        int? sprintNumber = null;
        DateTimeOffset? sprintEndsAt = null;
        if (goal.CurrentSprintId is Guid sid)
        {
            var sp = await _db.Sprints.Where(s => s.Id == sid)
                .Select(s => new { s.SequenceNumber, s.EndAt }).FirstOrDefaultAsync(ct);
            sprintNumber = sp?.SequenceNumber;
            sprintEndsAt = sp?.EndAt;
        }

        var s = goal.Settings;
        var dto = new GoalDetailDto(
            goal.Id, goal.Title, goal.Description, goal.JoinCode, goal.AdminUserId, goal.TimeZone,
            new GoalSettingsDto(
                s.SprintDurationDays, s.BaseXpTargetPerSprint, s.UnblockThresholdPct,
                s.FinalTriggerDaysBefore, s.FinalTriggerTargetPct, s.VoteApprovalThreshold,
                s.DebtCarryEnabled, s.XpScalableEasy, s.XpScalableMedium, s.XpScalableHard,
                s.BlockedApps.Select(a => new BlockedAppDto(a.PackageName, a.DisplayName)).ToList()),
            goal.CurrentSprintId,
            sprintNumber,
            sprintEndsAt);

        return Result.Success(dto);
    }
}
