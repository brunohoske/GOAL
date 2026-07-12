using Goal.Application.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class DevicesController : ApiControllerBase
{
    /// <summary>Registers (or reactivates) the device's FCM token for push notifications.</summary>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceCommand cmd) => ToResult(await Mediator.Send(cmd));

    /// <summary>Deactivates the device's FCM token (call on logout).</summary>
    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister(UnregisterDeviceCommand cmd) => ToResult(await Mediator.Send(cmd));
}
