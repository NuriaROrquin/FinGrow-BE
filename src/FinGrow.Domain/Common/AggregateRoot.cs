namespace FinGrow.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var domainEvents = _domainEvents.ToList();
        _domainEvents.Clear();

        return domainEvents;
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }
}
