using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Sprints;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Members;

/// <summary>Joins a goal using its shareable join code (case-insensitive). Returns the goal id.</summary>
public record JoinByCodeCommand(string Code) : IRequest<Result<Guid>>;

public class JoinByCodeValidator : AbstractValidator<JoinByCodeCommand>
{
    public JoinByCodeValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(12);
}

public class JoinByCodeHandler : IRequestHandler<JoinByCodeCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public JoinByCodeHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(JoinByCodeCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var code = cmd.Code.Trim().ToUpperInvariant();
        var goal = await _db.Goals
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.JoinCode == code && g.Status == GoalStatus.Active, ct);
        if (goal is null) return Error.NotFound("Nenhum GOAL encontrado com esse código.");

        var existing = await _db.GoalMembers
            .FirstOrDefaultAsync(m => m.GoalId == goal.Id && m.UserId == userId, ct);
        if (existing is not null)
        {
            if (existing.Status == MemberStatus.Active)
                return Error.Conflict("Você já participa deste GOAL.");
            // Rejoining after having left: reactivate instead of duplicating.
            existing.Status = MemberStatus.Active;
            await _db.SaveChangesAsync(ct);
            return Result.Success(goal.Id);
        }

        var member = new GoalMember
        {
            GoalId = goal.Id,
            UserId = userId,
            Role = MemberRole.Member,
            Status = MemberStatus.Active
        };
        _db.GoalMembers.Add(member);

        // Bootstrap the new member's state in the goal's active sprint.
        if (goal.CurrentSprintId is Guid sprintId)
        {
            var state = new SprintMemberState
            {
                SprintId = sprintId,
                GoalMemberId = member.Id,
                BaseTargetXp = goal.Settings.BaseXpTargetPerSprint,
                CarriedDebtXp = 0,
                EarnedXp = 0
            };
            state.RecalculateTargets(goal.Settings.UnblockThresholdPct);
            _db.SprintMemberStates.Add(state);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(goal.Id);
    }
}
