using Goal.Application.Abstractions;
using Goal.Domain.Identity;

namespace Goal.Application.Auth;

/// <summary>Shared helper: issues an access token and persists a rotating refresh token.</summary>
internal static class IssueTokens
{
    public static Task<AuthTokens> For(User user, IAppDbContext db, ITokenService tokens, CancellationToken ct)
    {
        var (access, accessExp) = tokens.CreateAccessToken(user);
        var (refresh, refreshHash, refreshExp) = tokens.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExp
        });

        return Task.FromResult(new AuthTokens(access, accessExp, refresh, user.Id, user.DisplayName));
    }
}
