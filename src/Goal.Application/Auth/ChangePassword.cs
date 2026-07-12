using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Auth;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _hasher;

    public ChangePasswordHandler(IAppDbContext db, ICurrentUser currentUser, IPasswordHasher hasher)
    {
        _db = db; _currentUser = currentUser; _hasher = hasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId)
            return Error.Unauthorized("Not authenticated.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Error.NotFound("User not found.");

        if (!_hasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            return Error.Validation("Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(cmd.NewPassword);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
