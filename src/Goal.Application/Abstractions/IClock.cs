namespace Goal.Application.Abstractions;

/// <summary>Abstraction over the system clock for deterministic, testable time-based logic.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
