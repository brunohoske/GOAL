namespace Goal.Application.Abstractions;

/// <summary>Provides the authenticated user's identity to the Application layer.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}
