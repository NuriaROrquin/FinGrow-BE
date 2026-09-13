namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Cuanto valia una inversion en una fecha.
/// </summary>
public sealed class InvestmentValuation : Entity
{
    private InvestmentValuation()
    {
    }

    private InvestmentValuation(
        Guid id,
        Guid investmentId,
        Money value,
        DateOnly valuedOn,
        ValuationSource source,
        DateTimeOffset createdAt)
        : base(id)
    {
        InvestmentId = investmentId;
        Value = value;
        ValuedOn = valuedOn;
        Source = source;
        CreatedAt = createdAt;
    }

    public Guid InvestmentId { get; private set; }

    public Money Value { get; private set; } = null!;

    public DateOnly ValuedOn { get; private set; }

    public ValuationSource Source { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static InvestmentValuation Create(
        Guid investmentId,
        Money value,
        DateOnly valuedOn,
        ValuationSource source,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new InvestmentValuation(Guid.CreateVersion7(), investmentId, value, valuedOn, source, createdAt);
    }
}
