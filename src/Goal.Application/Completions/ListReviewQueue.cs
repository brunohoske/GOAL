using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Completions;

public record ReviewAttachmentDto(int Type, string Url, string? FileName);

public record ReviewItemDto(
    Guid CompletionId,
    string TaskTitle,
    string AuthorName,
    string TextContent,
    IReadOnlyList<ReviewAttachmentDto> Attachments,
    int Approvals,
    int Rejections,
    int EligibleVoters,
    int ApprovalsNeeded,
    bool IHaveVoted);

/// <summary>
/// Completions pending review in a goal that the current user is eligible to vote on
/// (not their own, not already voted), with the documentation needed to judge them.
/// </summary>
public record ListReviewQueueQuery(Guid GoalId) : IRequest<Result<IReadOnlyList<ReviewItemDto>>>;

public class ListReviewQueueHandler : IRequestHandler<ListReviewQueueQuery, Result<IReadOnlyList<ReviewItemDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListReviewQueueHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ReviewItemDto>>> Handle(ListReviewQueueQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var me = await GoalAccess.FindMemberAsync(_db, q.GoalId, userId, ct);
        if (me is null) return Error.Forbidden("You are not a member of this goal.");

        var settings = await _db.GoalSettings.FirstAsync(s => s.GoalId == q.GoalId, ct);
        var activeMembers = await GoalAccess.CountActiveMembersAsync(_db, q.GoalId, ct);

        // Pending completions in this goal authored by someone else.
        var completions = await _db.TaskCompletions
            .Where(c => c.Status == CompletionStatus.PendingReview && c.SubmittedByGoalMemberId != me.Id)
            .Join(_db.SprintTaskAssignments, c => c.SprintTaskAssignmentId, a => a.Id, (c, a) => new { c, a })
            .Join(_db.Sprints, x => x.a.SprintId, s => s.Id, (x, s) => new { x.c, x.a, s })
            .Where(x => x.s.GoalId == q.GoalId)
            .Join(_db.TaskDefinitions, x => x.a.TaskDefinitionId, t => t.Id, (x, t) => new { x.c, x.a, TaskTitle = t.Title })
            .ToListAsync(ct);

        if (completions.Count == 0)
            return Result.Success<IReadOnlyList<ReviewItemDto>>(new List<ReviewItemDto>());

        var completionIds = completions.Select(x => x.c.Id).ToList();
        var authorMemberIds = completions.Select(x => x.c.SubmittedByGoalMemberId).Distinct().ToList();

        var authorNames = await _db.GoalMembers
            .Where(m => authorMemberIds.Contains(m.Id))
            .Join(_db.Users, m => m.UserId, u => u.Id, (m, u) => new { m.Id, u.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);

        var votes = await _db.CompletionVotes
            .Where(v => completionIds.Contains(v.TaskCompletionId))
            .ToListAsync(ct);

        var attachments = await _db.CompletionAttachments
            .Where(a => completionIds.Contains(a.TaskCompletionId))
            .ToListAsync(ct);

        var eligibleVoters = Math.Max(0, activeMembers - 1);
        var approvalsNeeded = (int)Math.Ceiling(eligibleVoters * settings.VoteApprovalThreshold);

        var result = completions.Select(x =>
        {
            var cVotes = votes.Where(v => v.TaskCompletionId == x.c.Id).ToList();
            return new ReviewItemDto(
                x.c.Id,
                x.TaskTitle,
                authorNames.TryGetValue(x.c.SubmittedByGoalMemberId, out var an) ? an : "Membro",
                x.c.TextContent,
                attachments.Where(a => a.TaskCompletionId == x.c.Id)
                    .Select(a => new ReviewAttachmentDto((int)a.Type, a.Url, a.FileName)).ToList(),
                cVotes.Count(v => v.Decision == VoteDecision.Approve),
                cVotes.Count(v => v.Decision == VoteDecision.Reject),
                eligibleVoters,
                approvalsNeeded,
                cVotes.Any(v => v.VoterGoalMemberId == me.Id));
        }).Where(r => !r.IHaveVoted).ToList();

        return Result.Success<IReadOnlyList<ReviewItemDto>>(result);
    }
}
