using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Common;
using Goal.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Blocking;

public record BlockedAppInfo(string PackageName, string DisplayName);

public record BlockingStateDto(
    bool IsBlocked,
    decimal CurrentPct,
    decimal TargetPct,
    int EarnedXp,
    int EffectiveTargetXp,
    int TargetXp,
    int UnblockThresholdXp,
    int DebtXp,
    int DaysRemaining,
    bool RequiresFullCompletion,
    int XpRemainingToUnblock,
    IReadOnlyList<BlockedAppInfo> BlockedApps,
    // Chaos-mode nags active right now (blocked + within the config's day window).
    bool RandomOverlayActive = false,
    bool TypingSabotageActive = false,
    string? TypingSabotageText = null);

public record GetBlockingStateQuery(Guid GoalId) : IRequest<Result<BlockingStateDto>>;

public class GetBlockingStateHandler : IRequestHandler<GetBlockingStateQuery, Result<BlockingStateDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetBlockingStateHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db; _currentUser = currentUser; _clock = clock;
    }

    public async Task<Result<BlockingStateDto>> Handle(GetBlockingStateQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var member = await GoalAccess.FindMemberAsync(_db, q.GoalId, userId, ct);
        if (member is null) return Error.Forbidden("You are not a member of this goal.");

        var goal = await _db.Goals
            .Include(g => g.Settings).ThenInclude(s => s.BlockedApps)
            .FirstOrDefaultAsync(g => g.Id == q.GoalId, ct);
        if (goal?.CurrentSprintId is null) return Error.NotFound("No active sprint.");

        // Archived (deleted) goal: nobody stays blocked by it — lets the phone clear its policy.
        if (goal.Status == GoalStatus.Archived)
            return Result.Success(new BlockingStateDto(
                false, 1m, 0m, 0, 0, 0, 0, 0, 0, false, 0, new List<BlockedAppInfo>()));

        var displayName = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.DisplayName).FirstAsync(ct);

        var sprint = await _db.Sprints.FirstAsync(sp => sp.Id == goal.CurrentSprintId, ct);
        var state = await _db.SprintMemberStates
            .FirstOrDefaultAsync(ms => ms.SprintId == sprint.Id && ms.GoalMemberId == member.Id, ct);
        if (state is null) return Error.NotFound("No sprint state for this member.");

        var tz = ResolveTz(goal.TimeZone);
        var bs = BlockingStateCalculator.Calculate(state, sprint, goal.Settings, _clock.UtcNow, tz);

        var apps = goal.Settings.BlockedApps
            .Select(a => new BlockedAppInfo(a.PackageName, a.DisplayName))
            .ToList();

        // A nag is active only while blocked AND within its configured "days before end" window.
        var st = goal.Settings;
        var overlayActive = bs.IsBlocked && st.RandomOverlayEnabled &&
                            bs.DaysRemaining <= st.RandomOverlayDaysBefore;
        var typingActive = bs.IsBlocked && st.TypingSabotageEnabled &&
                           bs.DaysRemaining <= st.TypingSabotageDaysBefore;

        var typingText = typingActive
            ? ResolveTemplate(st.TypingSabotageText, bs.XpRemainingToUnblock, displayName)
            : null;

        return Result.Success(new BlockingStateDto(
            bs.IsBlocked, bs.CurrentPct, bs.TargetPct, bs.EarnedXp, bs.EffectiveTargetXp,
            bs.TargetXp, bs.UnblockThresholdXp, bs.DebtXp, bs.DaysRemaining,
            bs.RequiresFullCompletion, bs.XpRemainingToUnblock, apps,
            overlayActive, typingActive, typingText));
    }

    private static string ResolveTemplate(string? template, int xpRemaining, string name)
    {
        var t = string.IsNullOrWhiteSpace(template)
            ? "{nome}, faltam {xp} XP. Larga o celular."
            : template;
        return t.Replace("{xp}", xpRemaining.ToString()).Replace("{nome}", name);
    }

    private static TimeZoneInfo ResolveTz(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
