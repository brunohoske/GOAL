using Goal.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => ToResult(await Mediator.Send(new ListNotificationsQuery()));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId)
        => ToResult(await Mediator.Send(new MarkNotificationReadCommand(notificationId)));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
        => ToResult(await Mediator.Send(new MarkAllNotificationsReadCommand()));
}
