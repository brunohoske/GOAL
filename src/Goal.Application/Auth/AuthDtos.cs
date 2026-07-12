namespace Goal.Application.Auth;

public record AuthTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, Guid UserId, string DisplayName);

public record UserDto(Guid Id, string Email, string DisplayName, string? AvatarUrl);
