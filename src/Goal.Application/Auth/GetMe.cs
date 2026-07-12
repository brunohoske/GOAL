using Goal.Application.Abstractions;
using Goal.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Auth;

/// <summary>Returns the current user's profile.</summary>
public record GetMeQuery : IRequest<Result<UserDto>>;

public class GetMeHandler : IRequestHandler<GetMeQuery, Result<UserDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMeHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetMeQuery q, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto(u.Id, u.Email, u.DisplayName, u.AvatarUrl))
            .FirstOrDefaultAsync(ct);

        return user is null ? Error.NotFound("User not found.") : Result.Success(user);
    }
}
