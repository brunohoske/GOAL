using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Application.Notifications;
using Goal.Domain.Common;
using Goal.Domain.Completions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Completions;

public record AttachmentInput(AttachmentType Type, string Url, string? FileName, string? ContentType, long? SizeBytes);
public record ChecklistInput(Guid ChecklistItemTemplateId, bool IsChecked);

public record SubmitCompletionCommand(
    Guid AssignmentId,
    string TextContent,
    IReadOnlyList<AttachmentInput> Attachments,
    IReadOnlyList<ChecklistInput> Checklist) : IRequest<Result<Guid>>;

public class SubmitCompletionValidator : AbstractValidator<SubmitCompletionCommand>
{
    public SubmitCompletionValidator()
    {
        RuleFor(x => x.TextContent).NotEmpty().WithMessage("Documentation text is required.");
    }
}

/// <summary>
/// Submits documentation to complete an assignment. Validates the snapshotted doc requirements
/// (text always; image/attachment/checklist as flagged), enters PendingReview, computes the
/// review deadline and schedules the deadline-resolution job.
/// </summary>
public class SubmitCompletionHandler : IRequestHandler<SubmitCompletionCommand, Result<Guid>>
{
    private const int ReviewWindowHours = 48;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IJobScheduler _jobs;
    private readonly Notifier _notifier;

    public SubmitCompletionHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock, IJobScheduler jobs, Notifier notifier)
    {
        _db = db; _currentUser = currentUser; _clock = clock; _jobs = jobs; _notifier = notifier;
    }

    public async Task<Result<Guid>> Handle(SubmitCompletionCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var assignment = await _db.SprintTaskAssignments
            .Include(a => a.Sprint)
            .Include(a => a.TaskDefinition)
            .FirstOrDefaultAsync(a => a.Id == cmd.AssignmentId, ct);
        if (assignment is null) return Error.NotFound("Assignment not found.");

        var member = await GoalAccess.FindMemberAsync(_db, assignment.Sprint!.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");
        if (assignment.AssignedToGoalMemberId != member.Id)
            return Error.Forbidden("This assignment is not assigned to you.");

        // --- Validate documentation requirements from the snapshot ---
        if (assignment.SnapshotRequiresImage && !cmd.Attachments.Any(a => a.Type == AttachmentType.Image))
            return Error.Validation("This task requires an image.");
        if (assignment.SnapshotRequiresAttachment &&
            !cmd.Attachments.Any(a => a.Type is AttachmentType.File or AttachmentType.Link))
            return Error.Validation("This task requires an attachment or link.");
        if (assignment.SnapshotHasChecklist)
        {
            var requiredItems = await _db.ChecklistItemTemplates
                .Where(t => t.TaskDefinitionId == assignment.TaskDefinitionId && t.IsRequired)
                .Select(t => t.Id).ToListAsync(ct);
            var checked_ = cmd.Checklist.Where(c => c.IsChecked).Select(c => c.ChecklistItemTemplateId).ToHashSet();
            if (requiredItems.Any(id => !checked_.Contains(id)))
                return Error.Validation("All required checklist items must be completed.");
        }

        var now = _clock.UtcNow;
        var completion = new TaskCompletion
        {
            SprintTaskAssignmentId = assignment.Id,
            SubmittedByGoalMemberId = member.Id,
            TextContent = cmd.TextContent,
            Status = CompletionStatus.PendingReview,
            SubmittedAt = now,
            ReviewDeadlineAt = now.AddHours(ReviewWindowHours),
            DeliveredOnTime = assignment.DueAt is null || now <= assignment.DueAt,
            Attempt = 1
        };
        foreach (var a in cmd.Attachments)
            completion.Attachments.Add(new CompletionAttachment
            { Type = a.Type, Url = a.Url, FileName = a.FileName, ContentType = a.ContentType, SizeBytes = a.SizeBytes });
        foreach (var c in cmd.Checklist)
            completion.ChecklistStates.Add(new CompletionChecklistState
            { ChecklistItemTemplateId = c.ChecklistItemTemplateId, IsChecked = c.IsChecked });

        assignment.Status = AssignmentStatus.PendingReview;
        _db.TaskCompletions.Add(completion);

        // Ask the other members to review (they are the voters).
        var author = await _db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).FirstAsync(ct);
        var taskTitle = assignment.TaskDefinition?.Title ?? "Tarefa";
        var reviewers = await _db.GoalMembers
            .Where(m => m.GoalId == assignment.Sprint!.GoalId && m.Status == MemberStatus.Active && m.Id != member.Id)
            .ToListAsync(ct);
        var data = new Dictionary<string, string>
        {
            ["goalId"] = assignment.Sprint!.GoalId.ToString(),
            ["type"] = "ReviewRequested"
        };
        foreach (var reviewer in reviewers)
            await _notifier.NotifyAsync(reviewer, assignment.Sprint!.GoalId, NotificationType.ReviewRequested,
                "Revisão pendente", $"{author} concluiu \"{taskTitle}\". Vote para aprovar.", data, ct);

        await _db.SaveChangesAsync(ct);

        _jobs.ScheduleCompletionDeadline(completion.Id, completion.ReviewDeadlineAt);
        return Result.Success(completion.Id);
    }
}
