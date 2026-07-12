using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Assignments;
using Goal.Domain.Services;
using Goal.Domain.Sprints;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Sprints;

public record CloseSprintCommand(Guid SprintId) : IRequest<Result>;

/// <summary>
/// Closes a sprint: computes each member's end debt, opens the next sprint with carried debt,
/// re-instances unfinished (non-approved) assignments as backlog. Idempotent via status guard.
/// Called by the SprintCloserJob (or manually by an admin).
/// </summary>
public class CloseSprintHandler : IRequestHandler<CloseSprintCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CloseSprintHandler(IAppDbContext db, IClock clock)
    {
        _db = db; _clock = clock;
    }

    public async Task<Result> Handle(CloseSprintCommand cmd, CancellationToken ct)
    {
        var sprint = await _db.Sprints
            .Include(s => s.MemberStates)
            .FirstOrDefaultAsync(s => s.Id == cmd.SprintId, ct);
        if (sprint is null) return Result.Failure(Error.NotFound("Sprint not found."));
        if (sprint.Status == SprintStatus.Closed) return Result.Success(); // idempotent

        var goal = await _db.Goals.Include(g => g.Settings).FirstAsync(g => g.Id == sprint.GoalId, ct);

        sprint.Status = SprintStatus.Closing;

        var nextSprint = new Sprint
        {
            GoalId = sprint.GoalId,
            SequenceNumber = sprint.SequenceNumber + 1,
            StartAt = sprint.EndAt,
            EndAt = sprint.EndAt.AddDays(goal.Settings.SprintDurationDays),
            Status = SprintStatus.Active
        };

        // Per-member: compute debt and create next state carrying it forward.
        foreach (var state in sprint.MemberStates)
        {
            var outcome = SprintDebtCalculator.Close(state);
            state.EndDebtXp = outcome.EndDebtXp;
            state.ReachedThreshold = outcome.ReachedThreshold;

            var nextState = SprintDebtCalculator.CreateNextState(
                nextSprint.Id, state.GoalMemberId, outcome, goal.Settings);
            nextSprint.MemberStates.Add(nextState);
        }

        // Carry unfinished assignments into the new sprint as backlog.
        var unfinished = await _db.SprintTaskAssignments
            .Where(a => a.SprintId == sprint.Id &&
                        a.Status != AssignmentStatus.Approved &&
                        a.Status != AssignmentStatus.CarriedToBacklog)
            .ToListAsync(ct);

        foreach (var a in unfinished)
        {
            a.Status = AssignmentStatus.CarriedToBacklog;
            nextSprint.GoalId = sprint.GoalId; // ensure set
            _db.SprintTaskAssignments.Add(Rollover(a, nextSprint.Id, sprint.Id));
        }

        sprint.Status = SprintStatus.Closed;
        sprint.ClosedAt = _clock.UtcNow;

        _db.Sprints.Add(nextSprint);
        goal.CurrentSprintId = nextSprint.Id;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static SprintTaskAssignment Rollover(SprintTaskAssignment a, Guid nextSprintId, Guid originSprintId) => new()
    {
        SprintId = nextSprintId,
        TaskDefinitionId = a.TaskDefinitionId,
        AssignedToGoalMemberId = a.AssignedToGoalMemberId,
        AssignmentType = a.AssignmentType,
        Status = AssignmentStatus.Open,
        DueAt = null,
        IsBacklog = true,
        OriginSprintId = a.OriginSprintId ?? originSprintId,
        SnapshotXpMode = a.SnapshotXpMode,
        SnapshotManualXp = a.SnapshotManualXp,
        SnapshotDifficulty = a.SnapshotDifficulty,
        SnapshotOnTimeBonusXp = a.SnapshotOnTimeBonusXp,
        SnapshotStreakBonusXp = a.SnapshotStreakBonusXp,
        SnapshotRequiresText = a.SnapshotRequiresText,
        SnapshotRequiresImage = a.SnapshotRequiresImage,
        SnapshotRequiresAttachment = a.SnapshotRequiresAttachment,
        SnapshotHasChecklist = a.SnapshotHasChecklist
    };
}
