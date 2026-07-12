using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Goals;
using Goal.Domain.Sprints;
using MediatR;

namespace Goal.Application.Goals;

/// <summary>
/// Creates a Goal with its immutable settings + blocked apps, makes the creator the admin,
/// and bootstraps Sprint #1 with the admin's SprintMemberState. This is where almost all of
/// the goal's behaviour is parameterised (nothing hard-coded).
/// </summary>
public record CreateGoalCommand(
    string Title,
    string? Description,
    string TimeZone,
    GoalSettingsDto Settings,
    DateTimeOffset? StartAt) : IRequest<Result<Guid>>;

public class CreateGoalValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.TimeZone).NotEmpty();
        RuleFor(x => x.Settings.SprintDurationDays).InclusiveBetween(1, 90);
        RuleFor(x => x.Settings.BaseXpTargetPerSprint).GreaterThan(0);
        RuleFor(x => x.Settings.UnblockThresholdPct).InclusiveBetween(0.05m, 1.0m);
        RuleFor(x => x.Settings.FinalTriggerDaysBefore).InclusiveBetween(0, 30);
        RuleFor(x => x.Settings.FinalTriggerTargetPct).InclusiveBetween(0.05m, 1.0m);
        RuleFor(x => x.Settings.VoteApprovalThreshold).InclusiveBetween(0.01m, 1.0m);
        RuleForEach(x => x.Settings.BlockedApps).ChildRules(a =>
        {
            a.RuleFor(p => p.PackageName).NotEmpty();
            a.RuleFor(p => p.DisplayName).NotEmpty();
        });
    }
}

public class CreateGoalHandler : IRequestHandler<CreateGoalCommand, Result<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateGoalHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db; _currentUser = currentUser; _clock = clock;
    }

    public async Task<Result<Guid>> Handle(CreateGoalCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid adminUserId)
            return Error.Unauthorized("Not authenticated.");

        if (!TryGetTimeZone(cmd.TimeZone, out var tz))
            return Error.Validation($"Unknown timezone '{cmd.TimeZone}'.");

        var s = cmd.Settings;
        var goal = new GoalAggregate
        {
            Title = cmd.Title,
            Description = cmd.Description,
            JoinCode = await GenerateUniqueJoinCodeAsync(ct),
            AdminUserId = adminUserId,
            TimeZone = cmd.TimeZone,
            Status = GoalStatus.Active
        };
        goal.Settings = new GoalSettings
        {
            GoalId = goal.Id,
            SprintDurationDays = s.SprintDurationDays,
            BaseXpTargetPerSprint = s.BaseXpTargetPerSprint,
            UnblockThresholdPct = s.UnblockThresholdPct,
            FinalTriggerDaysBefore = s.FinalTriggerDaysBefore,
            FinalTriggerTargetPct = s.FinalTriggerTargetPct,
            VoteApprovalThreshold = s.VoteApprovalThreshold,
            DebtCarryEnabled = s.DebtCarryEnabled,
            XpScalableEasy = s.XpScalableEasy,
            XpScalableMedium = s.XpScalableMedium,
            XpScalableHard = s.XpScalableHard,
            BlockedApps = s.BlockedApps
                .Select(a => new GoalBlockedApp { PackageName = a.PackageName, DisplayName = a.DisplayName })
                .ToList()
        };

        var admin = new GoalMember
        {
            GoalId = goal.Id,
            UserId = adminUserId,
            Role = MemberRole.Admin,
            Status = MemberStatus.Active
        };
        goal.Members.Add(admin);

        // Bootstrap sprint #1 (start anchored to the goal's timezone day boundary).
        var startAt = cmd.StartAt ?? _clock.UtcNow;
        var sprint = new Sprint
        {
            GoalId = goal.Id,
            SequenceNumber = 1,
            StartAt = startAt,
            EndAt = startAt.AddDays(s.SprintDurationDays),
            Status = SprintStatus.Active
        };
        var adminState = new SprintMemberState
        {
            SprintId = sprint.Id,
            GoalMemberId = admin.Id,
            BaseTargetXp = s.BaseXpTargetPerSprint,
            CarriedDebtXp = 0,
            EarnedXp = 0
        };
        adminState.RecalculateTargets(s.UnblockThresholdPct);
        sprint.MemberStates.Add(adminState);
        goal.Sprints.Add(sprint);
        goal.CurrentSprintId = sprint.Id;

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync(ct);
        return Result.Success(goal.Id);
    }

    private static bool TryGetTimeZone(string id, out TimeZoneInfo tz)
    {
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch { tz = TimeZoneInfo.Utc; return false; }
    }

    // Unambiguous alphabet (no 0/O, 1/I/L) so codes are easy to read aloud and type.
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private async Task<string> GenerateUniqueJoinCodeAsync(CancellationToken ct)
    {
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6)
                .Select(_ => CodeAlphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(CodeAlphabet.Length)])
                .ToArray());
            if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .AnyAsync(_db.Goals, g => g.JoinCode == code, ct))
                return code;
        }
    }
}
