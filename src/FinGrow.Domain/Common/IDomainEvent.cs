namespace FinGrow.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
