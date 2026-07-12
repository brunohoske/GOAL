using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Goals;

/// <summary>
/// Admin-only edit of the FEW mutable goal fields. Everything else stays immutable by design.
/// - Title: applies immediately.
/// - SprintDurationDays: applies to the NEXT sprint only (the current one keeps its end date,
///   because sprint creation reads the settings at close time).
/// - BaseXpTargetPerSprint (+ derived XP table): applies NOW — each member's base target is
///   updated in the current sprint while their EarnedXp and carried debt are preserved.
/// </summary>
public record UpdateGoalCommand(
    Guid GoalId,
    string? Title,
    int? SprintDurationDays,
    int? BaseXpTargetPerSprint,
    int? XpScalableEasy,
    int? XpScalableMedium,
    int? XpScalableHard,
    bool? RandomOverlayEnabled = null,
    int? RandomOverlayDaysBefore = null,
    bool? TypingSabotageEnabled = null,
    int? TypingSabotageDaysBefore = null,
    string? TypingSabotageText = null) : IRequest<Result>;

public class UpdateGoalValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120).When(x => x.Title is not null);
        RuleFor(x => x.SprintDurationDays!.Value).InclusiveBetween(1, 90).When(x => x.SprintDurationDays.HasValue);
        RuleFor(x => x.BaseXpTargetPerSprint!.Value).GreaterThan(0).When(x => x.BaseXpTargetPerSprint.HasValue);
        RuleFor(x => x.XpScalableEasy!.Value).GreaterThan(0).When(x => x.XpScalableEasy.HasValue);
        RuleFor(x => x.XpScalableMedium!.Value).GreaterThan(0).When(x => x.XpScalableMedium.HasValue);
        RuleFor(x => x.XpScalableHard!.Value).GreaterThan(0).When(x => x.XpScalableHard.HasValue);
        RuleFor(x => x.RandomOverlayDaysBefore!.Value).InclusiveBetween(0, 90).When(x => x.RandomOverlayDaysBefore.HasValue);
        RuleFor(x => x.TypingSabotageDaysBefore!.Value).InclusiveBetween(0, 90).When(x => x.TypingSabotageDaysBefore.HasValue);
        RuleFor(x => x.TypingSabotageText!).MaximumLength(280).When(x => x.TypingSabotageText is not null);
    }
}

public class UpdateGoalHandler : IRequestHandler<UpdateGoalCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateGoalHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateGoalCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var actor = await GoalAccess.FindMemberAsync(_db, cmd.GoalId, userId, ct);
        if (actor is null || actor.Role != MemberRole.Admin)
            return Error.Forbidden("Somente o admin pode editar o GOAL.");

        var goal = await _db.Goals.Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.Id == cmd.GoalId && g.Status == GoalStatus.Active, ct);
        if (goal is null) return Error.NotFound("GOAL não encontrado.");

        if (cmd.Title is not null) goal.Title = cmd.Title.Trim();

        // Next sprint only: CloseSprint reads this value when creating the next sprint.
        if (cmd.SprintDurationDays is int days) goal.Settings.SprintDurationDays = days;

        if (cmd.XpScalableEasy is int e) goal.Settings.XpScalableEasy = e;
        if (cmd.XpScalableMedium is int m) goal.Settings.XpScalableMedium = m;
        if (cmd.XpScalableHard is int h) goal.Settings.XpScalableHard = h;

        // Chaos-mode nags: can be toggled/adjusted after creation (deliberate exception).
        if (cmd.RandomOverlayEnabled is bool ro) goal.Settings.RandomOverlayEnabled = ro;
        if (cmd.RandomOverlayDaysBefore is int rod) goal.Settings.RandomOverlayDaysBefore = rod;
        if (cmd.TypingSabotageEnabled is bool ts) goal.Settings.TypingSabotageEnabled = ts;
        if (cmd.TypingSabotageDaysBefore is int tsd) goal.Settings.TypingSabotageDaysBefore = tsd;
        if (cmd.TypingSabotageText is not null)
            goal.Settings.TypingSabotageText =
                string.IsNullOrWhiteSpace(cmd.TypingSabotageText) ? null : cmd.TypingSabotageText.Trim();

        // Applies now: rebase every member's target in the CURRENT sprint, preserving
        // earned XP and carried debt (effective target = new base + existing debt).
        if (cmd.BaseXpTargetPerSprint is int target)
        {
            goal.Settings.BaseXpTargetPerSprint = target;
            if (goal.CurrentSprintId is Guid sprintId)
            {
                var states = await _db.SprintMemberStates
                    .Where(s => s.SprintId == sprintId).ToListAsync(ct);
                foreach (var state in states)
                {
                    state.BaseTargetXp = target;
                    state.RecalculateTargets(goal.Settings.UnblockThresholdPct);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
