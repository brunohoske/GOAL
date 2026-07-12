using Goal.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult ToResult<T>(Result<T> result)
        => result.IsSuccess ? Ok(result.Value) : Problem(result.Error!);

    protected IActionResult ToResult(Result result)
        => result.IsSuccess ? NoContent() : Problem(result.Error!);

    protected IActionResult ToCreated<T>(Result<T> result, string? location = null)
        => result.IsSuccess ? Created(location ?? string.Empty, result.Value) : Problem(result.Error!);

    private IActionResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(detail: error.Message, statusCode: status);
    }
}
