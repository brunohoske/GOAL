using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Assignments;
using Goal.Domain.Common;
using Goal.Domain.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Assignments;

/// <summary>
/// Creates a sprint assignment from a catalog task. If TargetMemberId is null the caller
/// self-assigns (any member); if set, only the admin may assign it to that member.
/// Snapshots the task's XP/flags so later edits can't rewrite history.
/// </summary>
public record AssignTaskCommand(Guid SprintId, Guid TaskDefinitionId, Guid? TargetMemberId, DateTimeOffset? DueAt)
    : IRequest<Result<Guid>>;

public class AssignTaskHandler : IRequestHandler<AssignTaskCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AssignTaskHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AssignTaskCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == cmd.SprintId, ct);
        if (sprint is null) return Error.NotFound("Sprint not found.");

        var actor = await GoalAccess.FindMemberAsync(_db, sprint.GoalId, userId, ct);
        if (actor is null) return Error.Forbidden("You are not a member of this goal.");

        var task = await _db.TaskDefinitions
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskDefinitionId && t.GoalId == sprint.GoalId && t.IsActive, ct);
        if (task is null) return Error.NotFound("Task not found in this goal.");

        Guid assigneeMemberId;
        AssignmentType type;
        if (cmd.TargetMemberId is Guid targetMemberId && targetMemberId != actor.Id)
        {
            if (actor.Role != MemberRole.Admin)
                return Error.Forbidden("Only the admin can assign tasks to other members.");
            var target = await _db.GoalMembers.FirstOrDefaultAsync(
                m => m.Id == targetMemberId && m.GoalId == sprint.GoalId && m.Status == MemberStatus.Active, ct);
            if (target is null) return Error.NotFound("Target member not found.");
            assigneeMemberId = target.Id;
            type = AssignmentType.AdminAssigned;
        }
        else
        {
            assigneeMemberId = actor.Id;
            type = AssignmentType.SelfAssigned;
        }

        // Idempotency guard: repeated taps (or retried requests) must not stack duplicate
        // assignments. If this member already holds an open assignment for the same task in
        // this sprint, return it instead of creating another one.
        var existing = await _db.SprintTaskAssignments.FirstOrDefaultAsync(a =>
            a.SprintId == sprint.Id
            && a.TaskDefinitionId == task.Id
            && a.AssignedToGoalMemberId == assigneeMemberId
            && (a.Status == AssignmentStatus.Open
                || a.Status == AssignmentStatus.InProgress
                || a.Status == AssignmentStatus.PendingReview), ct);
        if (existing is not null) return Result.Success(existing.Id);

        var assignment = new SprintTaskAssignment
        {
            SprintId = sprint.Id,
            TaskDefinitionId = task.Id,
            AssignedToGoalMemberId = assigneeMemberId,
            AssignmentType = type,
            Status = AssignmentStatus.InProgress,
            DueAt = cmd.DueAt ?? sprint.EndAt,
            IsBacklog = false,
            SnapshotXpMode = task.XpMode,
            SnapshotManualXp = task.ManualXp,
            SnapshotDifficulty = task.Difficulty,
            SnapshotOnTimeBonusXp = task.OnTimeBonusXp,
            SnapshotStreakBonusXp = task.StreakBonusXp,
            SnapshotRequiresText = task.RequiresText,
            SnapshotRequiresImage = task.RequiresImage,
            SnapshotRequiresAttachment = task.RequiresAttachment,
            SnapshotHasChecklist = task.HasChecklist
        };
        _db.SprintTaskAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return Result.Success(assignment.Id);
    }
}
