using Goal.Domain.Assignments;
using Goal.Domain.Sprints;
using Goal.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Goal.Infrastructure.Persistence.Configurations;

public class TaskDefinitionConfiguration : IEntityTypeConfiguration<TaskDefinition>
{
    public void Configure(EntityTypeBuilder<TaskDefinition> e)
    {
        e.ToTable("task_definitions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Title).IsRequired();
        e.HasIndex(x => new { x.GoalId, x.IsActive });
        e.HasOne(x => x.Goal).WithMany().HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.ChecklistItems).WithOne(x => x.TaskDefinition!).HasForeignKey(x => x.TaskDefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChecklistItemTemplateConfiguration : IEntityTypeConfiguration<ChecklistItemTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistItemTemplate> e)
    {
        e.ToTable("checklist_item_templates");
        e.HasKey(x => x.Id);
        e.Property(x => x.Label).IsRequired();
        e.HasIndex(x => x.TaskDefinitionId);
    }
}

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> e)
    {
        e.ToTable("sprints");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.GoalId, x.SequenceNumber }).IsUnique();
        e.HasIndex(x => new { x.Status, x.EndAt }); // SprintCloserJob scans Active sprints past EndAt
        e.HasMany(x => x.MemberStates).WithOne(x => x.Sprint!).HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SprintMemberStateConfiguration : IEntityTypeConfiguration<SprintMemberState>
{
    public void Configure(EntityTypeBuilder<SprintMemberState> e)
    {
        e.ToTable("sprint_member_states");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.SprintId, x.GoalMemberId }).IsUnique();
        e.HasOne(x => x.GoalMember).WithMany().HasForeignKey(x => x.GoalMemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class XpLedgerEntryConfiguration : IEntityTypeConfiguration<XpLedgerEntry>
{
    public void Configure(EntityTypeBuilder<XpLedgerEntry> e)
    {
        e.ToTable("xp_ledger_entries");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.GoalMemberId, x.SprintId });
        e.HasOne(x => x.GoalMember).WithMany().HasForeignKey(x => x.GoalMemberId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Sprint).WithMany().HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SprintTaskAssignmentConfiguration : IEntityTypeConfiguration<SprintTaskAssignment>
{
    public void Configure(EntityTypeBuilder<SprintTaskAssignment> e)
    {
        e.ToTable("sprint_task_assignments");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.SprintId, x.Status });
        e.HasIndex(x => x.AssignedToGoalMemberId);
        e.HasOne(x => x.Sprint).WithMany().HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.TaskDefinition).WithMany().HasForeignKey(x => x.TaskDefinitionId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.AssignedToGoalMember).WithMany().HasForeignKey(x => x.AssignedToGoalMemberId).OnDelete(DeleteBehavior.SetNull);
    }
}
