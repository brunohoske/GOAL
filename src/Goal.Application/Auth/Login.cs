using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Auth;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokens>>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthTokens>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db; _hasher = hasher; _tokens = tokens;
    }

    public async Task<Result<AuthTokens>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == cmd.Email, ct);
        if (user is null || !_hasher.Verify(cmd.Password, user.PasswordHash))
            return Error.Unauthorized("Invalid email or password.");

        var tokens = await IssueTokens.For(user, _db, _tokens, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(tokens);
    }
}
