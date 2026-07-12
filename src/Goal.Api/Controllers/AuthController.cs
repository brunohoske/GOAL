using Goal.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

public class AuthController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand cmd) => ToResult(await Mediator.Send(cmd));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand cmd) => ToResult(await Mediator.Send(cmd));

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenCommand cmd) => ToResult(await Mediator.Send(cmd));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand cmd) => ToResult(await Mediator.Send(cmd));

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me() => ToResult(await Mediator.Send(new GetMeQuery()));
}
