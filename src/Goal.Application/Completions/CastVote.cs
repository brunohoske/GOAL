using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Completions;
using Goal.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Completions;

public record VoteResultDto(string Outcome, int Approvals, int Rejections, int EligibleVoters, int ApprovalsNeeded);

public record CastVoteCommand(Guid CompletionId, VoteDecision Decision, string? Comment) : IRequest<Result<VoteResultDto>>;

/// <summary>
/// Records a member's vote (one per member, author excluded) and triggers resolution.
/// If the tally crosses the threshold (or becomes impossible), the completion resolves immediately.
/// </summary>
public class CastVoteHandler : IRequestHandler<CastVoteCommand, Result<VoteResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly CompletionResolver _resolver;

    public CastVoteHandler(IAppDbContext db, ICurrentUser currentUser, CompletionResolver resolver)
    {
        _db = db; _currentUser = currentUser; _resolver = resolver;
    }

    public async Task<Result<VoteResultDto>> Handle(CastVoteCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var completion = await _db.TaskCompletions
            .Include(c => c.SprintTaskAssignment)!.ThenInclude(a => a!.Sprint)
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == cmd.CompletionId, ct);
        if (completion is null) return Error.NotFound("Completion not found.");
        if (completion.Status != CompletionStatus.PendingReview)
            return Error.Conflict("This completion is no longer open for voting.");

        var goalId = completion.SprintTaskAssignment!.Sprint!.GoalId;
        var voter = await GoalAccess.FindMemberAsync(_db, goalId, userId, ct);
        if (voter is null) return Error.Forbidden("You are not a member of this goal.");
        if (voter.Id == completion.SubmittedByGoalMemberId)
            return Error.Forbidden("You cannot vote on your own completion.");
        if (completion.Votes.Any(v => v.VoterGoalMemberId == voter.Id))
            return Error.Conflict("You have already voted on this completion.");

        _db.CompletionVotes.Add(new CompletionVote
        {
            TaskCompletionId = completion.Id,
            VoterGoalMemberId = voter.Id,
            Decision = cmd.Decision,
            Comment = cmd.Comment
        });
        await _db.SaveChangesAsync(ct);

        // Attempt early resolution (no force — only resolves if mathematically decided).
        var outcome = await _resolver.TryResolveAsync(completion.Id, force: false, ct);

        var approvals = completion.Votes.Count(v => v.Decision == VoteDecision.Approve) +
                        (cmd.Decision == VoteDecision.Approve ? 1 : 0);
        var rejections = completion.Votes.Count(v => v.Decision == VoteDecision.Reject) +
                         (cmd.Decision == VoteDecision.Reject ? 1 : 0);
        var activeMembers = await GoalAccess.CountActiveMembersAsync(_db, goalId, ct);
        var eligible = Math.Max(0, activeMembers - 1);
        var settings = await _db.GoalSettings.FirstAsync(s => s.GoalId == goalId, ct);
        var needed = (int)Math.Ceiling(eligible * settings.VoteApprovalThreshold);

        return Result.Success(new VoteResultDto(outcome.ToString(), approvals, rejections, eligible, needed));
    }
}
