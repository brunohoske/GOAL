namespace Goal.Infrastructure.Services;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "goal-api";
    public string Audience { get; set; } = "goal-app";
    public string SigningKey { get; set; } = default!;          // >= 32 bytes; from config/secret
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
