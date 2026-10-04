namespace FinGrow.Domain.UnitTests.Common;

using FinGrow.Domain.Common;

public class AggregateRootTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Raised_events_come_out_in_the_order_they_happened()
    {
        var aggregate = new SampleAggregate();

        aggregate.Touch("primero");
        aggregate.Touch("segundo");

        var labels = aggregate.DequeueDomainEvents()
            .Cast<SampleTouched>()
            .Select(domainEvent => domainEvent.Label)
            .ToList();

        labels.Count.ShouldBe(2);
        labels[0].ShouldBe("primero");
        labels[1].ShouldBe("segundo");
    }

    [Fact]
    public void Dequeuing_empties_the_queue_so_an_event_is_handled_only_once()
    {
        var aggregate = new SampleAggregate();
        aggregate.Touch("unico");

        aggregate.DequeueDomainEvents().ShouldHaveSingleItem();

        aggregate.DequeueDomainEvents().ShouldBeEmpty();
    }

    private sealed record SampleTouched(string Label, DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class SampleAggregate : AggregateRoot
    {
        public SampleAggregate() : base(Guid.CreateVersion7()) { }

        public void Touch(string label) => Raise(new SampleTouched(label, Now));
    }
}
