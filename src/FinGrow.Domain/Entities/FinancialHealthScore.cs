namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class FinancialHealthScore : AggregateRoot
{
    private FinancialHealthScore()
    {
    }

    private FinancialHealthScore(
        Guid id,
        Guid employeeId,
        MetricsPeriod period,
        FinancialHealthFactors factors,
        DateTimeOffset computedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        PeriodStart = period.Start;
        Factors = factors;
        Score = factors.Score;
        ComputedAt = computedAt;
    }

    public Guid EmployeeId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public FinancialHealthFactors Factors { get; private set; } = null!;

    public int Score { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    public MetricsPeriod Period => MetricsPeriod.FromStart(PeriodStart);

    public FinancialHealthBand Band => FinancialHealthScale.BandOf(Score);

    public static FinancialHealthScore Compute(
        Guid employeeId,
        MetricsPeriod period,
        FinancialHealthFactors factors,
        DateTimeOffset computedAt)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(factors);

        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Un puntaje de salud financiera siempre pertenece a un empleado.");
        }

        return new FinancialHealthScore(Guid.CreateVersion7(), employeeId, period, factors, computedAt);
    }

    public void Refresh(FinancialHealthFactors factors, DateTimeOffset computedAt)
    {
        ArgumentNullException.ThrowIfNull(factors);

        Factors = factors;
        Score = factors.Score;
        ComputedAt = computedAt;
    }
}
