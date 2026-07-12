using Goal.Application.Completions;
using Goal.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class CompletionsController : ApiControllerBase
{
    [HttpPost("~/api/v1/assignments/{assignmentId:guid}/completions")]
    public async Task<IActionResult> Submit(Guid assignmentId, SubmitCompletionBody body)
        => ToCreated(await Mediator.Send(new SubmitCompletionCommand(
            assignmentId, body.TextContent, body.Attachments ?? new(), body.Checklist ?? new())));

    [HttpPost("{completionId:guid}/votes")]
    public async Task<IActionResult> Vote(Guid completionId, VoteBody body)
        => ToResult(await Mediator.Send(new CastVoteCommand(completionId, body.Decision, body.Comment)));
}

public record SubmitCompletionBody(
    string TextContent,
    List<AttachmentInput>? Attachments,
    List<ChecklistInput>? Checklist);

public record VoteBody(VoteDecision Decision, string? Comment);
