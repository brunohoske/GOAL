using Goal.Application.Abstractions;
using Goal.Domain.Assignments;
using Goal.Domain.Completions;
using Goal.Domain.Goals;
using Goal.Domain.Identity;
using Goal.Domain.Notifications;
using Goal.Domain.Sprints;
using Goal.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Goal.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<GoalAggregate> Goals => Set<GoalAggregate>();
    public DbSet<GoalSettings> GoalSettings => Set<GoalSettings>();
    public DbSet<GoalBlockedApp> GoalBlockedApps => Set<GoalBlockedApp>();
    public DbSet<GoalMember> GoalMembers => Set<GoalMember>();
    public DbSet<GoalInvite> GoalInvites => Set<GoalInvite>();

    public DbSet<TaskDefinition> TaskDefinitions => Set<TaskDefinition>();
    public DbSet<ChecklistItemTemplate> ChecklistItemTemplates => Set<ChecklistItemTemplate>();

    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintMemberState> SprintMemberStates => Set<SprintMemberState>();
    public DbSet<XpLedgerEntry> XpLedgerEntries => Set<XpLedgerEntry>();

    public DbSet<SprintTaskAssignment> SprintTaskAssignments => Set<SprintTaskAssignment>();

    public DbSet<TaskCompletion> TaskCompletions => Set<TaskCompletion>();
    public DbSet<CompletionAttachment> CompletionAttachments => Set<CompletionAttachment>();
    public DbSet<CompletionChecklistState> CompletionChecklistStates => Set<CompletionChecklistState>();
    public DbSet<CompletionVote> CompletionVotes => Set<CompletionVote>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationSchedule> NotificationSchedules => Set<NotificationSchedule>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.HasPostgresExtension("citext");
        b.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
