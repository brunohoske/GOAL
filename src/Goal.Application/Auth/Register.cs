using FluentValidation;
using Goal.Application.Abstractions;
using Goal.Application.Common;
using Goal.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Auth;

public record RegisterCommand(string Email, string DisplayName, string Password, string? AvatarUrl) : IRequest<Result<AuthTokens>>;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthTokens>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public RegisterHandler(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db; _hasher = hasher; _tokens = tokens;
    }

    public async Task<Result<AuthTokens>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Email == cmd.Email, ct))
            return Error.Conflict("Email already registered.");

        var user = new User
        {
            Email = cmd.Email,
            DisplayName = cmd.DisplayName,
            PasswordHash = _hasher.Hash(cmd.Password),
            AvatarUrl = cmd.AvatarUrl
        };
        _db.Users.Add(user);

        var tokens = await IssueTokens.For(user, _db, _tokens, ct);
        await _db.SaveChangesAsync(ct);
        return Result.Success(tokens);
    }
}
