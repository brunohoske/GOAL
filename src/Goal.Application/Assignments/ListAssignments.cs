using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Assignments;

public record AssignmentDto(
    Guid Id,
    Guid TaskDefinitionId,
    string TaskTitle,
    AssignmentStatus Status,
    Guid? AssignedToMemberId,
    string? AssignedToName,
    bool AssignedToMe,
    bool IsBacklog,
    int EstimatedXp,
    bool RequiresImage,
    bool RequiresAttachment,
    bool HasChecklist,
    // --- Latest completion / voting snapshot (null when nothing was submitted yet) ---
    Guid? CompletionId,
    int Approvals,
    int Rejections,
    int ApprovalsNeeded,
    int? MyVote,      // 0 = approved, 1 = rejected, null = didn't vote
    int? AwardedXp);

/// <summary>Lists this sprint's assignments (what's been picked up / assigned), with assignee info.</summary>
public record ListAssignmentsQuery(Guid SprintId) : IRequest<Result<IReadOnlyList<AssignmentDto>>>;

public class ListAssignmentsHandler : IRequestHandler<ListAssignmentsQuery, Result<IReadOnlyList<AssignmentDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListAssignmentsHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<AssignmentDto>>> Handle(ListAssignmentsQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == q.SprintId, ct);
        if (sprint is null) return Error.NotFound("Sprint not found.");

        var me = await GoalAccess.FindMemberAsync(_db, sprint.GoalId, userId, ct);
        if (me is null) return Error.Forbidden("You are not a member of this goal.");

        var settings = await _db.GoalSettings.FirstAsync(s => s.GoalId == sprint.GoalId, ct);

        var rows = await _db.SprintTaskAssignments
            .Where(a => a.SprintId == q.SprintId)
            .Join(_db.TaskDefinitions, a => a.TaskDefinitionId, t => t.Id, (a, t) => new { a, t })
            .ToListAsync(ct);

        // Resolve assignee display names.
        var memberIds = rows.Where(r => r.a.AssignedToGoalMemberId != null)
            .Select(r => r.a.AssignedToGoalMemberId!.Value).Distinct().ToList();
        var names = await _db.GoalMembers
            .Where(m => memberIds.Contains(m.Id))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m, u) => new { m.Id, u.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);

        // Latest completion per assignment, with its votes (feeds the per-member sprint board).
        var assignmentIds = rows.Select(r => r.a.Id).ToList();
        var latestCompletions = (await _db.TaskCompletions
                .Where(c => assignmentIds.Contains(c.SprintTaskAssignmentId))
                .Include(c => c.Votes)
                .ToListAsync(ct))
            .GroupBy(c => c.SprintTaskAssignmentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.SubmittedAt).First());

        var activeMembers = await GoalAccess.CountActiveMembersAsync(_db, sprint.GoalId, ct);
        var eligibleVoters = Math.Max(0, activeMembers - 1);
        var approvalsNeeded = (int)Math.Ceiling(eligibleVoters * settings.VoteApprovalThreshold);

        var result = rows.Select(r =>
        {
            latestCompletions.TryGetValue(r.a.Id, out var completion);
            var myVote = completion?.Votes.FirstOrDefault(v => v.VoterGoalMemberId == me.Id);
            return new AssignmentDto(
                r.a.Id,
                r.t.Id,
                r.t.Title,
                r.a.Status,
                r.a.AssignedToGoalMemberId,
                r.a.AssignedToGoalMemberId is Guid mid && names.TryGetValue(mid, out var n) ? n : null,
                r.a.AssignedToGoalMemberId == me.Id,
                r.a.IsBacklog,
                EstimateXp(r.a.SnapshotXpMode, r.a.SnapshotManualXp, r.a.SnapshotDifficulty, settings),
                r.a.SnapshotRequiresImage,
                r.a.SnapshotRequiresAttachment,
                r.a.SnapshotHasChecklist,
                completion?.Id,
                completion?.Votes.Count(v => v.Decision == VoteDecision.Approve) ?? 0,
                completion?.Votes.Count(v => v.Decision == VoteDecision.Reject) ?? 0,
                approvalsNeeded,
                myVote is null ? null : (int)myVote.Decision,
                completion?.AwardedXp);
        }).ToList();

        return Result.Success<IReadOnlyList<AssignmentDto>>(result);
    }

    private static int EstimateXp(XpMode mode, int? manual, Difficulty? diff, Domain.Goals.GoalSettings s) => mode switch
    {
        XpMode.Manual => manual ?? 0,
        XpMode.Scalable => s.XpBaseFor(diff ?? Difficulty.Easy),
        _ => 0
    };
}
