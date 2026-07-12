using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Members;

public record MemberDto(
    Guid MemberId,
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    bool IsAdmin,
    int EarnedXp,
    int EffectiveTargetXp,
    bool IsMe);

public record ListMembersQuery(Guid GoalId) : IRequest<Result<IReadOnlyList<MemberDto>>>;

public class ListMembersHandler : IRequestHandler<ListMembersQuery, Result<IReadOnlyList<MemberDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMembersHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MemberDto>>> Handle(ListMembersQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var me = await GoalAccess.FindMemberAsync(_db, q.GoalId, userId, ct);
        if (me is null) return Error.Forbidden("You are not a member of this goal.");

        var goal = await _db.Goals.FirstAsync(g => g.Id == q.GoalId, ct);

        var members = await _db.GoalMembers
            .Where(m => m.GoalId == q.GoalId && m.Status == MemberStatus.Active)
            .Join(_db.Users, m => m.UserId, u => u.Id, (m, u) => new { m, u })
            .ToListAsync(ct);

        // Pull current-sprint state for earned/target per member (if a sprint is active).
        var states = goal.CurrentSprintId is Guid sid
            ? await _db.SprintMemberStates.Where(s => s.SprintId == sid).ToListAsync(ct)
            : new();

        var result = members.Select(x =>
        {
            var st = states.FirstOrDefault(s => s.GoalMemberId == x.m.Id);
            return new MemberDto(
                x.m.Id, x.u.Id, x.u.DisplayName, x.u.AvatarUrl,
                x.m.Role == MemberRole.Admin,
                st?.EarnedXp ?? 0, st?.EffectiveTargetXp ?? 0,
                x.u.Id == userId);
        }).OrderByDescending(m => m.EarnedXp).ToList();

        return Result.Success<IReadOnlyList<MemberDto>>(result);
    }
}
