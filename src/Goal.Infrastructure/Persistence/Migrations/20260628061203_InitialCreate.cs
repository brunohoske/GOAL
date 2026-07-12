using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Goal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "goal_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintDurationDays = table.Column<int>(type: "integer", nullable: false),
                    BaseXpTargetPerSprint = table.Column<int>(type: "integer", nullable: false),
                    UnblockThresholdPct = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    FinalTriggerDaysBefore = table.Column<int>(type: "integer", nullable: false),
                    FinalTriggerTargetPct = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    VoteApprovalThreshold = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    DebtCarryEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    XpScalableEasy = table.Column<int>(type: "integer", nullable: false),
                    XpScalableMedium = table.Column<int>(type: "integer", nullable: false),
                    XpScalableHard = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "citext", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "goal_blocked_apps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_blocked_apps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goal_blocked_apps_goal_settings_GoalSettingsId",
                        column: x => x.GoalSettingsId,
                        principalTable: "goal_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FcmToken = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentSprintId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    SettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goals_goal_settings_SettingsId",
                        column: x => x.SettingsId,
                        principalTable: "goal_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goals_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goal_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedEmail = table.Column<string>(type: "citext", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_invites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goal_invites_goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goal_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goal_members_goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goal_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sprints_goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    XpMode = table.Column<int>(type: "integer", nullable: false),
                    ManualXp = table.Column<int>(type: "integer", nullable: true),
                    Difficulty = table.Column<int>(type: "integer", nullable: true),
                    OnTimeBonusXp = table.Column<int>(type: "integer", nullable: true),
                    StreakBonusXp = table.Column<int>(type: "integer", nullable: true),
                    RequiresText = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresImage = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresAttachment = table.Column<bool>(type: "boolean", nullable: false),
                    HasChecklist = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_definitions_goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_goal_members_GoalMemberId",
                        column: x => x.GoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    NextFireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_schedules_goal_members_GoalMemberId",
                        column: x => x.GoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_schedules_sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprint_member_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseTargetXp = table.Column<int>(type: "integer", nullable: false),
                    CarriedDebtXp = table.Column<int>(type: "integer", nullable: false),
                    EffectiveTargetXp = table.Column<int>(type: "integer", nullable: false),
                    EarnedXp = table.Column<int>(type: "integer", nullable: false),
                    UnblockThresholdXp = table.Column<int>(type: "integer", nullable: false),
                    EndDebtXp = table.Column<int>(type: "integer", nullable: true),
                    ReachedThreshold = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprint_member_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sprint_member_states_goal_members_GoalMemberId",
                        column: x => x.GoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sprint_member_states_sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xp_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceCompletionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xp_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_xp_ledger_entries_goal_members_GoalMemberId",
                        column: x => x.GoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_xp_ledger_entries_sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "checklist_item_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_item_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checklist_item_templates_task_definitions_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "task_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprint_task_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToGoalMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsBacklog = table.Column<bool>(type: "boolean", nullable: false),
                    OriginSprintId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotXpMode = table.Column<int>(type: "integer", nullable: false),
                    SnapshotManualXp = table.Column<int>(type: "integer", nullable: true),
                    SnapshotDifficulty = table.Column<int>(type: "integer", nullable: true),
                    SnapshotOnTimeBonusXp = table.Column<int>(type: "integer", nullable: true),
                    SnapshotStreakBonusXp = table.Column<int>(type: "integer", nullable: true),
                    SnapshotRequiresText = table.Column<bool>(type: "boolean", nullable: false),
                    SnapshotRequiresImage = table.Column<bool>(type: "boolean", nullable: false),
                    SnapshotRequiresAttachment = table.Column<bool>(type: "boolean", nullable: false),
                    SnapshotHasChecklist = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprint_task_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sprint_task_assignments_goal_members_AssignedToGoalMemberId",
                        column: x => x.AssignedToGoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sprint_task_assignments_sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sprint_task_assignments_task_definitions_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "task_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintTaskAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByGoalMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextContent = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewDeadlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveredOnTime = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AwardedXp = table.Column<int>(type: "integer", nullable: true),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_completions_goal_members_SubmittedByGoalMemberId",
                        column: x => x.SubmittedByGoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_completions_sprint_task_assignments_SprintTaskAssignme~",
                        column: x => x.SprintTaskAssignmentId,
                        principalTable: "sprint_task_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "completion_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskCompletionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completion_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_completion_attachments_task_completions_TaskCompletionId",
                        column: x => x.TaskCompletionId,
                        principalTable: "task_completions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "completion_checklist_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskCompletionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistItemTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completion_checklist_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_completion_checklist_states_checklist_item_templates_Checkl~",
                        column: x => x.ChecklistItemTemplateId,
                        principalTable: "checklist_item_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_completion_checklist_states_task_completions_TaskCompletion~",
                        column: x => x.TaskCompletionId,
                        principalTable: "task_completions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "completion_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskCompletionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterGoalMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completion_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_completion_votes_goal_members_VoterGoalMemberId",
                        column: x => x.VoterGoalMemberId,
                        principalTable: "goal_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_completion_votes_task_completions_TaskCompletionId",
                        column: x => x.TaskCompletionId,
                        principalTable: "task_completions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checklist_item_templates_TaskDefinitionId",
                table: "checklist_item_templates",
                column: "TaskDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_completion_attachments_TaskCompletionId",
                table: "completion_attachments",
                column: "TaskCompletionId");

            migrationBuilder.CreateIndex(
                name: "IX_completion_checklist_states_ChecklistItemTemplateId",
                table: "completion_checklist_states",
                column: "ChecklistItemTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_completion_checklist_states_TaskCompletionId",
                table: "completion_checklist_states",
                column: "TaskCompletionId");

            migrationBuilder.CreateIndex(
                name: "IX_completion_votes_TaskCompletionId_VoterGoalMemberId",
                table: "completion_votes",
                columns: new[] { "TaskCompletionId", "VoterGoalMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_completion_votes_VoterGoalMemberId",
                table: "completion_votes",
                column: "VoterGoalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_FcmToken",
                table: "device_tokens",
                column: "FcmToken");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_UserId_IsActive",
                table: "device_tokens",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_goal_blocked_apps_GoalSettingsId_PackageName",
                table: "goal_blocked_apps",
                columns: new[] { "GoalSettingsId", "PackageName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goal_invites_GoalId",
                table: "goal_invites",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_goal_invites_Token",
                table: "goal_invites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goal_members_GoalId_UserId",
                table: "goal_members",
                columns: new[] { "GoalId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goal_members_UserId",
                table: "goal_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_goal_settings_GoalId",
                table: "goal_settings",
                column: "GoalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goals_AdminUserId",
                table: "goals",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_goals_SettingsId",
                table: "goals",
                column: "SettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_schedules_GoalMemberId",
                table: "notification_schedules",
                column: "GoalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_schedules_IsActive_NextFireAt",
                table: "notification_schedules",
                columns: new[] { "IsActive", "NextFireAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_schedules_SprintId",
                table: "notification_schedules",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_GoalMemberId_Status",
                table: "notifications",
                columns: new[] { "GoalMemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sprint_member_states_GoalMemberId",
                table: "sprint_member_states",
                column: "GoalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_sprint_member_states_SprintId_GoalMemberId",
                table: "sprint_member_states",
                columns: new[] { "SprintId", "GoalMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sprint_task_assignments_AssignedToGoalMemberId",
                table: "sprint_task_assignments",
                column: "AssignedToGoalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_sprint_task_assignments_SprintId_Status",
                table: "sprint_task_assignments",
                columns: new[] { "SprintId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sprint_task_assignments_TaskDefinitionId",
                table: "sprint_task_assignments",
                column: "TaskDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_sprints_GoalId_SequenceNumber",
                table: "sprints",
                columns: new[] { "GoalId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sprints_Status_EndAt",
                table: "sprints",
                columns: new[] { "Status", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_SprintTaskAssignmentId_Status",
                table: "task_completions",
                columns: new[] { "SprintTaskAssignmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_Status",
                table: "task_completions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_task_completions_SubmittedByGoalMemberId",
                table: "task_completions",
                column: "SubmittedByGoalMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_task_definitions_GoalId_IsActive",
                table: "task_definitions",
                columns: new[] { "GoalId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xp_ledger_entries_GoalMemberId_SprintId",
                table: "xp_ledger_entries",
                columns: new[] { "GoalMemberId", "SprintId" });

            migrationBuilder.CreateIndex(
                name: "IX_xp_ledger_entries_SprintId",
                table: "xp_ledger_entries",
                column: "SprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "completion_attachments");

            migrationBuilder.DropTable(
                name: "completion_checklist_states");

            migrationBuilder.DropTable(
                name: "completion_votes");

            migrationBuilder.DropTable(
                name: "device_tokens");

            migrationBuilder.DropTable(
                name: "goal_blocked_apps");

            migrationBuilder.DropTable(
                name: "goal_invites");

            migrationBuilder.DropTable(
                name: "notification_schedules");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sprint_member_states");

            migrationBuilder.DropTable(
                name: "xp_ledger_entries");

            migrationBuilder.DropTable(
                name: "checklist_item_templates");

            migrationBuilder.DropTable(
                name: "task_completions");

            migrationBuilder.DropTable(
                name: "sprint_task_assignments");

            migrationBuilder.DropTable(
                name: "goal_members");

            migrationBuilder.DropTable(
                name: "sprints");

            migrationBuilder.DropTable(
                name: "task_definitions");

            migrationBuilder.DropTable(
                name: "goals");

            migrationBuilder.DropTable(
                name: "goal_settings");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
