using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Common;

/// <summary>Helpers to resolve and authorize the current user's membership within a goal.</summary>
public static class GoalAccess
{
    public static Task<GoalMember?> FindMemberAsync(IAppDbContext db, Guid goalId, Guid userId, CancellationToken ct)
        => db.GoalMembers.FirstOrDefaultAsync(
            m => m.GoalId == goalId && m.UserId == userId && m.Status == MemberStatus.Active, ct);

    public static async Task<int> CountActiveMembersAsync(IAppDbContext db, Guid goalId, CancellationToken ct)
        => await db.GoalMembers.CountAsync(m => m.GoalId == goalId && m.Status == MemberStatus.Active, ct);
}
