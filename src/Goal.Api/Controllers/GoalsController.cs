using Goal.Application.Blocking;
using Goal.Application.Completions;
using Goal.Application.Goals;
using Goal.Application.Members;
using Goal.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class GoalsController : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateGoalCommand cmd) => ToCreated(await Mediator.Send(cmd));

    [HttpGet]
    public async Task<IActionResult> List() => ToResult(await Mediator.Send(new ListGoalsQuery()));

    [HttpGet("{goalId:guid}")]
    public async Task<IActionResult> Get(Guid goalId) => ToResult(await Mediator.Send(new GetGoalQuery(goalId)));

    /// <summary>Admin-only edit of the mutable fields (title, sprint duration, XP target/table).</summary>
    [HttpPatch("{goalId:guid}")]
    public async Task<IActionResult> Update(Guid goalId, UpdateGoalBody body)
        => ToResult(await Mediator.Send(new UpdateGoalCommand(
            goalId, body.Title, body.SprintDurationDays, body.BaseXpTargetPerSprint,
            body.XpScalableEasy, body.XpScalableMedium, body.XpScalableHard,
            body.RandomOverlayEnabled, body.RandomOverlayDaysBefore,
            body.TypingSabotageEnabled, body.TypingSabotageDaysBefore, body.TypingSabotageText)));

    /// <summary>Admin-only. Archives the goal; members are released automatically.</summary>
    [HttpDelete("{goalId:guid}")]
    public async Task<IActionResult> Delete(Guid goalId)
        => ToResult(await Mediator.Send(new DeleteGoalCommand(goalId)));

    [HttpGet("{goalId:guid}/blocking-state")]
    public async Task<IActionResult> BlockingState(Guid goalId)
        => ToResult(await Mediator.Send(new GetBlockingStateQuery(goalId)));

    [HttpGet("{goalId:guid}/tasks")]
    public async Task<IActionResult> ListTasks(Guid goalId)
        => ToResult(await Mediator.Send(new ListTasksQuery(goalId)));

    [HttpPost("{goalId:guid}/tasks")]
    public async Task<IActionResult> CreateTask(Guid goalId, CreateTaskDefinitionBody body)
        => ToCreated(await Mediator.Send(body.ToCommand(goalId)));

    [HttpGet("{goalId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid goalId)
        => ToResult(await Mediator.Send(new ListMembersQuery(goalId)));

    [HttpGet("{goalId:guid}/review-queue")]
    public async Task<IActionResult> ReviewQueue(Guid goalId)
        => ToResult(await Mediator.Send(new ListReviewQueueQuery(goalId)));

    [HttpPost("{goalId:guid}/invites")]
    public async Task<IActionResult> Invite(Guid goalId, InviteBody body)
        => ToResult(await Mediator.Send(new CreateInviteCommand(goalId, body.Email)));

    /// <summary>Joins a goal via its shareable code; returns the goal id.</summary>
    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinBody body)
        => ToResult(await Mediator.Send(new JoinByCodeCommand(body.Code)));
}

public record InviteBody(string Email);
public record JoinBody(string Code);
public record UpdateGoalBody(
    string? Title,
    int? SprintDurationDays,
    int? BaseXpTargetPerSprint,
    int? XpScalableEasy,
    int? XpScalableMedium,
    int? XpScalableHard,
    bool? RandomOverlayEnabled,
    int? RandomOverlayDaysBefore,
    bool? TypingSabotageEnabled,
    int? TypingSabotageDaysBefore,
    string? TypingSabotageText);

/// <summary>Body for creating a task (goalId comes from the route).</summary>
public record CreateTaskDefinitionBody(
    string Title,
    string? Description,
    Domain.Common.XpMode XpMode,
    int? ManualXp,
    Domain.Common.Difficulty? Difficulty,
    int? OnTimeBonusXp,
    int? StreakBonusXp,
    bool RequiresImage,
    bool RequiresAttachment,
    bool HasChecklist,
    List<ChecklistItemInput>? ChecklistItems)
{
    public CreateTaskDefinitionCommand ToCommand(Guid goalId) => new(
        goalId, Title, Description, XpMode, ManualXp, Difficulty, OnTimeBonusXp, StreakBonusXp,
        RequiresImage, RequiresAttachment, HasChecklist, ChecklistItems ?? new());
}
