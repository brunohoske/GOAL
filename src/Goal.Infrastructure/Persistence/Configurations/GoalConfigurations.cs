using Goal.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Goal.Infrastructure.Persistence.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<GoalAggregate>
{
    public void Configure(EntityTypeBuilder<GoalAggregate> e)
    {
        e.ToTable("goals");
        e.HasKey(x => x.Id);
        e.Property(x => x.Title).IsRequired();
        e.Property(x => x.TimeZone).IsRequired();
        e.Property(x => x.JoinCode).IsRequired().HasMaxLength(12);
        e.HasIndex(x => x.JoinCode).IsUnique();
        e.HasIndex(x => x.AdminUserId);

        e.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Settings).WithOne(x => x.Goal!).HasForeignKey<GoalSettings>(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Members).WithOne(x => x.Goal!).HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Sprints).WithOne(x => x.Goal!).HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoalSettingsConfiguration : IEntityTypeConfiguration<GoalSettings>
{
    public void Configure(EntityTypeBuilder<GoalSettings> e)
    {
        e.ToTable("goal_settings");
        e.HasKey(x => x.Id);
        e.Property(x => x.UnblockThresholdPct).HasColumnType("numeric(4,3)");
        e.Property(x => x.FinalTriggerTargetPct).HasColumnType("numeric(4,3)");
        e.Property(x => x.VoteApprovalThreshold).HasColumnType("numeric(4,3)");
        e.Property(x => x.TypingSabotageText).HasMaxLength(280);
        e.HasIndex(x => x.GoalId).IsUnique();
        // Navigation back to Goal is configured from the Goal side (HasOne(x => x.Settings)).
        e.Ignore(x => x.Goal);

        e.HasMany(x => x.BlockedApps).WithOne(x => x.GoalSettings!).HasForeignKey(x => x.GoalSettingsId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoalBlockedAppConfiguration : IEntityTypeConfiguration<GoalBlockedApp>
{
    public void Configure(EntityTypeBuilder<GoalBlockedApp> e)
    {
        e.ToTable("goal_blocked_apps");
        e.HasKey(x => x.Id);
        e.Property(x => x.PackageName).IsRequired();
        e.Property(x => x.DisplayName).IsRequired();
        e.HasIndex(x => new { x.GoalSettingsId, x.PackageName }).IsUnique();
    }
}

public class GoalMemberConfiguration : IEntityTypeConfiguration<GoalMember>
{
    public void Configure(EntityTypeBuilder<GoalMember> e)
    {
        e.ToTable("goal_members");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.GoalId, x.UserId }).IsUnique();
        e.Ignore(x => x.IsActive);
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoalInviteConfiguration : IEntityTypeConfiguration<GoalInvite>
{
    public void Configure(EntityTypeBuilder<GoalInvite> e)
    {
        e.ToTable("goal_invites");
        e.HasKey(x => x.Id);
        e.Property(x => x.InvitedEmail).HasColumnType("citext").IsRequired();
        e.Property(x => x.Token).IsRequired();
        e.HasIndex(x => x.Token).IsUnique();
        e.HasOne(x => x.Goal).WithMany().HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(x => x.CanBeAccepted);
    }
}
