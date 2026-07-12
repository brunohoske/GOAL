using System.Security.Cryptography;
using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Sprints;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Members;

// ---- Create invite (admin) ----
public record CreateInviteCommand(Guid GoalId, string Email) : IRequest<Result<string>>;

public class CreateInviteValidator : AbstractValidator<CreateInviteCommand>
{
    public CreateInviteValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public class CreateInviteHandler : IRequestHandler<CreateInviteCommand, Result<string>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateInviteHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db; _currentUser = currentUser; _clock = clock;
    }

    public async Task<Result<string>> Handle(CreateInviteCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var actor = await GoalAccess.FindMemberAsync(_db, cmd.GoalId, userId, ct);
        if (actor is null || actor.Role != MemberRole.Admin)
            return Error.Forbidden("Only the admin can invite members.");

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _db.GoalInvites.Add(new GoalInvite
        {
            GoalId = cmd.GoalId,
            InvitedEmail = cmd.Email,
            Token = token,
            Status = InviteStatus.Pending,
            ExpiresAt = _clock.UtcNow.AddDays(14),
            CreatedByUserId = userId
        });
        await _db.SaveChangesAsync(ct);
        return Result.Success(token);
    }
}

// ---- Accept invite ----
public record AcceptInviteCommand(string Token) : IRequest<Result<Guid>>;

public class AcceptInviteHandler : IRequestHandler<AcceptInviteCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AcceptInviteHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AcceptInviteCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var invite = await _db.GoalInvites.FirstOrDefaultAsync(i => i.Token == cmd.Token, ct);
        if (invite is null || !invite.CanBeAccepted)
            return Error.NotFound("Invite is invalid or expired.");

        if (await _db.GoalMembers.AnyAsync(m => m.GoalId == invite.GoalId && m.UserId == userId, ct))
            return Error.Conflict("You are already a member of this goal.");

        var member = new GoalMember
        {
            GoalId = invite.GoalId,
            UserId = userId,
            Role = MemberRole.Member,
            Status = MemberStatus.Active
        };
        _db.GoalMembers.Add(member);

        // Bootstrap the new member's state in the goal's active sprint.
        var goal = await _db.Goals.Include(g => g.Settings).FirstAsync(g => g.Id == invite.GoalId, ct);
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

        invite.Status = InviteStatus.Accepted;
        await _db.SaveChangesAsync(ct);
        return Result.Success(invite.GoalId);
    }
}
