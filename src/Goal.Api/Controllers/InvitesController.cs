using Goal.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class InvitesController : ApiControllerBase
{
    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token)
        => ToResult(await Mediator.Send(new AcceptInviteCommand(token)));
}
