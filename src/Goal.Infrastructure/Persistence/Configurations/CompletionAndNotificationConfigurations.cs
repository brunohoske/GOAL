using Goal.Domain.Completions;
using Goal.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Goal.Infrastructure.Persistence.Configurations;

public class TaskCompletionConfiguration : IEntityTypeConfiguration<TaskCompletion>
{
    public void Configure(EntityTypeBuilder<TaskCompletion> e)
    {
        e.ToTable("task_completions");
        e.HasKey(x => x.Id);
        e.Property(x => x.TextContent).IsRequired();
        e.HasIndex(x => new { x.SprintTaskAssignmentId, x.Status });
        e.HasIndex(x => x.Status);

        // PostgreSQL xmin as optimistic concurrency token — guards double-resolution of votes.
        e.Property(x => x.Version).IsRowVersion().HasColumnName("xmin").HasColumnType("xid");

        e.HasOne(x => x.SprintTaskAssignment).WithMany().HasForeignKey(x => x.SprintTaskAssignmentId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.SubmittedByGoalMember).WithMany().HasForeignKey(x => x.SubmittedByGoalMemberId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Attachments).WithOne(x => x.TaskCompletion!).HasForeignKey(x => x.TaskCompletionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.ChecklistStates).WithOne(x => x.TaskCompletion!).HasForeignKey(x => x.TaskCompletionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Votes).WithOne(x => x.TaskCompletion!).HasForeignKey(x => x.TaskCompletionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CompletionAttachmentConfiguration : IEntityTypeConfiguration<CompletionAttachment>
{
    public void Configure(EntityTypeBuilder<CompletionAttachment> e)
    {
        e.ToTable("completion_attachments");
        e.HasKey(x => x.Id);
        e.Property(x => x.Url).IsRequired();
    }
}

public class CompletionChecklistStateConfiguration : IEntityTypeConfiguration<CompletionChecklistState>
{
    public void Configure(EntityTypeBuilder<CompletionChecklistState> e)
    {
        e.ToTable("completion_checklist_states");
        e.HasKey(x => x.Id);
        e.HasOne(x => x.ChecklistItemTemplate).WithMany().HasForeignKey(x => x.ChecklistItemTemplateId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompletionVoteConfiguration : IEntityTypeConfiguration<CompletionVote>
{
    public void Configure(EntityTypeBuilder<CompletionVote> e)
    {
        e.ToTable("completion_votes");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.TaskCompletionId, x.VoterGoalMemberId }).IsUnique(); // one vote per member
        e.HasOne(x => x.VoterGoalMember).WithMany().HasForeignKey(x => x.VoterGoalMemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.ToTable("notifications");
        e.HasKey(x => x.Id);
        e.Property(x => x.Title).IsRequired();
        e.Property(x => x.Body).IsRequired();
        e.Property(x => x.DataJson).HasColumnType("jsonb");
        e.HasIndex(x => new { x.GoalMemberId, x.Status });
        e.HasOne(x => x.GoalMember).WithMany().HasForeignKey(x => x.GoalMemberId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationScheduleConfiguration : IEntityTypeConfiguration<NotificationSchedule>
{
    public void Configure(EntityTypeBuilder<NotificationSchedule> e)
    {
        e.ToTable("notification_schedules");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.IsActive, x.NextFireAt }); // escalation job scans active schedules due to fire
        e.HasOne(x => x.GoalMember).WithMany().HasForeignKey(x => x.GoalMemberId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Sprint).WithMany().HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.Cascade);
    }
}
