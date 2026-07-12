using Goal.Application.Completions;

namespace Goal.Infrastructure.Jobs;

/// <summary>Resolves a completion at its review deadline (forced decision on votes so far).</summary>
public sealed class CompletionDeadlineJob
{
    private readonly CompletionResolver _resolver;

    public CompletionDeadlineJob(CompletionResolver resolver) => _resolver = resolver;

    public Task RunAsync(Guid completionId) => _resolver.TryResolveAsync(completionId, force: true, CancellationToken.None);
}
