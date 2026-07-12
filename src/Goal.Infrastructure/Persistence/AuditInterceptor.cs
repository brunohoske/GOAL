using Goal.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Goal.Infrastructure.Persistence;

/// <summary>Stamps CreatedAt/UpdatedAt on entities automatically on save.</summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in context.ChangeTracker.Entries<Entity>())
            {
                if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
                if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAt = now;
            }
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
