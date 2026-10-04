namespace FinGrow.Infrastructure.Persistence;

using FinGrow.Application.Events;
using FinGrow.Domain.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly List<IDomainEvent> _pending = new();

    public DomainEventsInterceptor(IDomainEventDispatcher dispatcher) => _dispatcher = dispatcher;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is not null)
        {
            _pending.AddRange(eventData.Context.ChangeTracker
                .Entries<AggregateRoot>()
                .SelectMany(entry => entry.Entity.DequeueDomainEvents()));
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var domainEvents = _pending.ToList();
        _pending.Clear();

        if (domainEvents.Count > 0)
        {
            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pending.Clear();

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
