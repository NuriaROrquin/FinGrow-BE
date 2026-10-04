namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class PeriodMetricsTests
{
    [Fact]
    public void Participation_rate_is_the_share_of_active_employees_that_participated()
    {
        var metrics = PeriodMetrics.From(
            activeEmployees: 3,
            participatingEmployees: 1,
            confirmedTransactions: 4,
            employeesWithBudget: 2,
            employeesWithActiveGoal: 0,
            goalsAchieved: 1,
            employeesWithIntegration: 3);

        metrics.ParticipationRate.ShouldBe(33.33m);
    }

    [Fact]
    public void Without_active_employees_there_is_no_participation_rate()
    {
        PeriodMetrics.Empty.ParticipationRate.ShouldBeNull();
    }

    [Fact]
    public void Employee_counts_cannot_exceed_the_active_employees()
    {
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 3, 0, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 3, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 0, 3, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 0, 0, 0, 3));
    }

    [Fact]
    public void Negative_counts_are_rejected()
    {
        Should.Throw<DomainException>(() => PeriodMetrics.From(-1, 0, 0, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(1, 0, -1, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(1, 0, 0, 0, 0, -1, 0));
    }

    [Fact]
    public void Metrics_with_the_same_numbers_are_equal()
    {
        PeriodMetrics.From(3, 1, 4, 2, 0, 1, 3).ShouldBe(PeriodMetrics.From(3, 1, 4, 2, 0, 1, 3));
        PeriodMetrics.From(3, 1, 4, 2, 0, 1, 3).ShouldNotBe(PeriodMetrics.From(3, 2, 4, 2, 0, 1, 3));
    }
}
