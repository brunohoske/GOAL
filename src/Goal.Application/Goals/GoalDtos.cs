namespace Goal.Application.Goals;

public record BlockedAppDto(string PackageName, string DisplayName);

public record GoalSettingsDto(
    int SprintDurationDays,
    int BaseXpTargetPerSprint,
    decimal UnblockThresholdPct,
    int FinalTriggerDaysBefore,
    decimal FinalTriggerTargetPct,
    decimal VoteApprovalThreshold,
    bool DebtCarryEnabled,
    int XpScalableEasy,
    int XpScalableMedium,
    int XpScalableHard,
    IReadOnlyList<BlockedAppDto> BlockedApps);

public record GoalSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsAdmin,
    int? CurrentSprintNumber,
    DateTimeOffset? SprintEndsAt,
    int MemberCount);

public record GoalDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string JoinCode,
    Guid AdminUserId,
    string TimeZone,
    GoalSettingsDto Settings,
    Guid? CurrentSprintId,
    int? CurrentSprintNumber);
