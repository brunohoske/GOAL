using Goal.Application.Assignments;
using Goal.Application.Sprints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class SprintsController : ApiControllerBase
{
    [HttpGet("{sprintId:guid}/assignments")]
    public async Task<IActionResult> ListAssignments(Guid sprintId)
        => ToResult(await Mediator.Send(new ListAssignmentsQuery(sprintId)));

    [HttpPost("{sprintId:guid}/assignments")]
    public async Task<IActionResult> Assign(Guid sprintId, AssignBody body)
        => ToCreated(await Mediator.Send(new AssignTaskCommand(sprintId, body.TaskDefinitionId, body.TargetMemberId, body.DueAt)));

    [HttpPost("{sprintId:guid}/close")]
    public async Task<IActionResult> Close(Guid sprintId)
        => ToResult(await Mediator.Send(new CloseSprintCommand(sprintId)));
}

public record AssignBody(Guid TaskDefinitionId, Guid? TargetMemberId, DateTimeOffset? DueAt);
