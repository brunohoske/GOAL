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
}

public record InviteBody(string Email);

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
