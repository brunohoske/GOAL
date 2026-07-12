namespace Goal.Domain.Common;

/// <summary>
/// Base for all persisted entities. Uses UUID v7 PKs (time-ordered, great for index locality)
/// and tracks creation/update timestamps in UTC.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
