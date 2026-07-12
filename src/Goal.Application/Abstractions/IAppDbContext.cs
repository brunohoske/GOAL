using Goal.Domain.Assignments;
using Goal.Domain.Completions;
using Goal.Domain.Goals;
using Goal.Domain.Identity;
using Goal.Domain.Notifications;
using Goal.Domain.Sprints;
using Goal.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Abstractions;

/// <summary>Abstraction over the EF Core DbContext so the Application layer stays persistence-agnostic.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<GoalAggregate> Goals { get; }
    DbSet<GoalSettings> GoalSettings { get; }
    DbSet<GoalBlockedApp> GoalBlockedApps { get; }
    DbSet<GoalMember> GoalMembers { get; }
    DbSet<GoalInvite> GoalInvites { get; }

    DbSet<TaskDefinition> TaskDefinitions { get; }
    DbSet<ChecklistItemTemplate> ChecklistItemTemplates { get; }

    DbSet<Sprint> Sprints { get; }
    DbSet<SprintMemberState> SprintMemberStates { get; }
    DbSet<XpLedgerEntry> XpLedgerEntries { get; }

    DbSet<SprintTaskAssignment> SprintTaskAssignments { get; }

    DbSet<TaskCompletion> TaskCompletions { get; }
    DbSet<CompletionAttachment> CompletionAttachments { get; }
    DbSet<CompletionChecklistState> CompletionChecklistStates { get; }
    DbSet<CompletionVote> CompletionVotes { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationSchedule> NotificationSchedules { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
