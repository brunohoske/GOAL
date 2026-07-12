using Goal.Application.Abstractions;
using Goal.Application.Notifications;
using Goal.Domain.Common;
using Goal.Domain.Completions;
using Goal.Domain.Services;
using Goal.Domain.Sprints;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Completions;

/// <summary>
/// Shared resolution logic used by both the vote handler (early resolution) and the deadline job.
/// Tallies votes; on approval it computes XP from the snapshot, writes the XP ledger, increments
/// the member's EarnedXp (which feeds the blocking calculation), and marks the assignment.
/// Idempotent: returns early if the completion is no longer pending.
/// </summary>
public sealed class CompletionResolver
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly Notifier _notifier;

    public CompletionResolver(IAppDbContext db, IClock clock, Notifier notifier)
    {
        _db = db; _clock = clock; _notifier = notifier;
    }

    /// <summary>Attempts to resolve a completion. <paramref name="force"/>=true is used at the deadline.</summary>
    public async Task<VotingOutcome> TryResolveAsync(Guid completionId, bool force, CancellationToken ct)
    {
        var completion = await _db.TaskCompletions
            .Include(c => c.SprintTaskAssignment)!.ThenInclude(a => a!.Sprint)
            .Include(c => c.SprintTaskAssignment)!.ThenInclude(a => a!.TaskDefinition)
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == completionId, ct);

        if (completion is null || completion.Status != CompletionStatus.PendingReview)
            return VotingOutcome.Pending; // already resolved / gone — idempotent no-op

        var assignment = completion.SprintTaskAssignment!;
        var sprint = assignment.Sprint!;
        var goal = await _db.Goals.Include(g => g.Settings)
            .FirstAsync(g => g.Id == sprint.GoalId, ct);

        var activeMembers = await GoalAccessCount(sprint.GoalId, ct);
        var eligibleVoters = Math.Max(0, activeMembers - 1); // author doesn't vote

        var approvals = completion.Votes.Count(v => v.Decision == VoteDecision.Approve);
        var rejections = completion.Votes.Count(v => v.Decision == VoteDecision.Reject);

        var tally = VoteTally.Tally(eligibleVoters, approvals, rejections, goal.Settings.VoteApprovalThreshold);

        var outcome = tally.Outcome;
        if (outcome == VotingOutcome.Pending)
        {
            if (!force) return VotingOutcome.Pending;
            // At the deadline, decide on whatever approvals exist so far.
            outcome = approvals >= tally.ApprovalsNeeded ? VotingOutcome.Approved : VotingOutcome.Rejected;
        }

        if (outcome == VotingOutcome.Approved)
            await ApproveAsync(completion, assignment, sprint, goal.Settings, ct);
        else
            Reject(completion, assignment);

        completion.ResolvedAt = _clock.UtcNow;
        await NotifyAuthorAsync(completion, assignment, sprint, outcome, ct);
        await _db.SaveChangesAsync(ct);
        return outcome;
    }

    private async Task ApproveAsync(TaskCompletion completion, Domain.Assignments.SprintTaskAssignment assignment,
        Sprint sprint, Domain.Goals.GoalSettings settings, CancellationToken ct)
    {
        var state = await _db.SprintMemberStates
            .FirstAsync(ms => ms.SprintId == sprint.Id && ms.GoalMemberId == completion.SubmittedByGoalMemberId, ct);

        var qualifiesForStreak = false; // streak qualification can be refined later
        var award = XpCalculator.Calculate(assignment, settings, completion.DeliveredOnTime, qualifiesForStreak);

        AddLedger(completion, sprint, XpSourceType.TaskCompletion, award.Base);
        if (award.OnTimeBonus > 0) AddLedger(completion, sprint, XpSourceType.OnTimeBonus, award.OnTimeBonus);
        if (award.StreakBonus > 0) AddLedger(completion, sprint, XpSourceType.StreakBonus, award.StreakBonus);

        state.EarnedXp += award.Total;
        completion.AwardedXp = award.Total;
        completion.Status = CompletionStatus.Approved;
        assignment.Status = AssignmentStatus.Approved;
    }

    private static void Reject(TaskCompletion completion, Domain.Assignments.SprintTaskAssignment assignment)
    {
        completion.Status = CompletionStatus.Rejected;
        assignment.Status = AssignmentStatus.Rejected; // member can resubmit (new attempt)
    }

    private async Task NotifyAuthorAsync(TaskCompletion completion,
        Domain.Assignments.SprintTaskAssignment assignment, Sprint sprint, VotingOutcome outcome, CancellationToken ct)
    {
        var author = await _db.GoalMembers.FirstOrDefaultAsync(m => m.Id == completion.SubmittedByGoalMemberId, ct);
        if (author is null) return;

        var taskTitle = assignment.TaskDefinition?.Title ?? "Tarefa";
        var data = new Dictionary<string, string>
        {
            ["goalId"] = sprint.GoalId.ToString(),
            ["completionId"] = completion.Id.ToString(),
            ["type"] = outcome == VotingOutcome.Approved ? "CompletionApproved" : "CompletionRejected"
        };

        if (outcome == VotingOutcome.Approved)
            await _notifier.NotifyAsync(author, sprint.GoalId, NotificationType.CompletionApproved,
                "Tarefa aprovada! 🎉", $"\"{taskTitle}\" foi aprovada: +{completion.AwardedXp} XP.", data, ct);
        else
            await _notifier.NotifyAsync(author, sprint.GoalId, NotificationType.CompletionRejected,
                "Tarefa reprovada", $"\"{taskTitle}\" não foi aprovada. Revise a documentação e reenvie.", data, ct);
    }

    private void AddLedger(TaskCompletion completion, Sprint sprint, XpSourceType source, int amount)
        => _db.XpLedgerEntries.Add(new XpLedgerEntry
        {
            GoalMemberId = completion.SubmittedByGoalMemberId,
            SprintId = sprint.Id,
            SourceType = source,
            SourceCompletionId = completion.Id,
            Amount = amount
        });

    private Task<int> GoalAccessCount(Guid goalId, CancellationToken ct)
        => _db.GoalMembers.CountAsync(m => m.GoalId == goalId && m.Status == MemberStatus.Active, ct);
}
