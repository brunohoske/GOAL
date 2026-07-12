using Goal.Application.Abstractions;
using Hangfire;

namespace Goal.Infrastructure.Jobs;

public sealed class HangfireJobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireJobScheduler(IBackgroundJobClient jobs) => _jobs = jobs;

    public void ScheduleCompletionDeadline(Guid completionId, DateTimeOffset runAt)
        => _jobs.Schedule<CompletionDeadlineJob>(j => j.RunAsync(completionId), runAt);
}
