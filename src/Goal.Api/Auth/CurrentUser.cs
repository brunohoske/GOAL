using System.Security.Claims;
using Goal.Application.Abstractions;

namespace Goal.Api.Auth;

/// <summary>Reads the authenticated user id from the JWT 'sub' claim.</summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var sub = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? _accessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
