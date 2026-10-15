namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class FinancialHealthScoreTests
{
    private static readonly DateTimeOffset ComputedAt = new(2026, 9, 1, 4, 0, 0, TimeSpan.Zero);
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);

    [Fact]
    public void A_score_keeps_the_period_its_factors_and_when_it_was_computed()
    {
        var employeeId = Guid.CreateVersion7();
        var factors = FinancialHealthFactors.From(savings: 50m, budget: 100m, goals: 0m, cushion: 100m);

        var score = FinancialHealthScore.Compute(employeeId, August, factors, ComputedAt);

        score.EmployeeId.ShouldBe(employeeId);
        score.PeriodStart.ShouldBe(new DateOnly(2026, 8, 1));
        score.Period.ShouldBe(August);
        score.Factors.ShouldBe(factors);
        score.Score.ShouldBe(60);
        score.Band.ShouldBe(FinancialHealthBand.Stable);
        score.ComputedAt.ShouldBe(ComputedAt);
    }

    [Fact]
    public void Refreshing_recomputes_the_score_from_the_new_factors()
    {
        var score = FinancialHealthScore.Compute(
            Guid.CreateVersion7(),
            August,
            FinancialHealthFactors.From(10m, null, null, 0m),
            ComputedAt);
        var updated = FinancialHealthFactors.From(100m, 100m, null, 100m);

        score.Refresh(updated, ComputedAt.AddDays(3));

        score.Factors.ShouldBe(updated);
        score.Score.ShouldBe(100);
        score.Band.ShouldBe(FinancialHealthBand.Healthy);
        score.ComputedAt.ShouldBe(ComputedAt.AddDays(3));
        score.Period.ShouldBe(August);
    }

    [Fact]
    public void A_score_without_an_employee_is_rejected()
    {
        Should.Throw<DomainException>(() => FinancialHealthScore.Compute(
            Guid.Empty,
            August,
            FinancialHealthFactors.From(10m, null, null, 0m),
            ComputedAt));
    }
}
