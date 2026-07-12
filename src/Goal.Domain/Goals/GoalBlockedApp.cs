using Goal.Domain.Common;

namespace Goal.Domain.Goals;

/// <summary>An app that should be blocked for under-target members (e.g. Instagram).</summary>
public class GoalBlockedApp : Entity
{
    public Guid GoalSettingsId { get; set; }
    public GoalSettings? GoalSettings { get; set; }

    public string PackageName { get; set; } = default!;   // e.g. com.instagram.android
    public string DisplayName { get; set; } = default!;   // e.g. Instagram
}
