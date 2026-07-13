using Goal.Domain.Common;

namespace Goal.Domain.Goals;

/// <summary>An app that should be blocked for under-target members (e.g. Instagram).</summary>
public class GoalBlockedApp : Entity
{
    public Guid GoalSettingsId { get; set; }
    public GoalSettings? GoalSettings { get; set; }

    // e.g. com.instagram.android. Pseudo-packages target a feature instead of the whole app
    // (com.google.android.youtube:shorts = only the Shorts player); the Android enforcer knows them.
    public string PackageName { get; set; } = default!;
    public string DisplayName { get; set; } = default!;   // e.g. Instagram
}
