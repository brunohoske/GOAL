using Goal.Application.Abstractions;
using Goal.Application.Sprints;
using Goal.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Goal.Infrastructure.Jobs;

/// <summary>Recurring: closes sprints whose EndAt has passed. Idempotent (CloseSprint guards status).</summary>
public sealed class SprintCloserJob
{
    private readonly IAppDbContext _db;
    private readonly ISender _mediator;
    private readonly IClock _clock;

    public SprintCloserJob(IAppDbContext db, ISender mediator, IClock clock)
    {
        _db = db; _mediator = mediator; _clock = clock;
    }

    public async Task RunAsync()
    {
        var now = _clock.UtcNow;
        var dueSprintIds = await _db.Sprints
            .Where(s => s.Status == SprintStatus.Active && s.EndAt <= now)
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var id in dueSprintIds)
            await _mediator.Send(new CloseSprintCommand(id));
    }
}
