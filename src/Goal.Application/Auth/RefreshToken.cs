using Goal.Application.Abstractions;
using Goal.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Application.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokens>>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokens>>
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokens;

    public RefreshTokenHandler(IAppDbContext db, ITokenService tokens)
    {
        _db = db; _tokens = tokens;
    }

    public async Task<Result<AuthTokens>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = _tokens.HashRefreshToken(cmd.RefreshToken);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
            return Error.Unauthorized("Invalid or expired refresh token.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId, ct);
        if (user is null)
            return Error.Unauthorized("User no longer exists.");

        // Rotate: revoke the old token and chain it to the new one.
        var tokens = await IssueTokens.For(user, _db, _tokens, ct);
        existing.RevokedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(tokens);
    }
}
