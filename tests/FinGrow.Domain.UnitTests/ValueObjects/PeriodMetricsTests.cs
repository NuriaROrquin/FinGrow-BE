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
            employeesWithIntegration: 3,
            employeesWithScore: 0,
            financialHealthScoreSum: 0);

        metrics.ParticipationRate.ShouldBe(33.33m);
    }

    [Fact]
    public void Without_active_employees_there_is_no_participation_rate()
    {
        PeriodMetrics.Empty.ParticipationRate.ShouldBeNull();
    }

    [Fact]
    public void The_average_score_is_the_sum_of_scores_over_the_employees_that_have_one()
    {
        var metrics = PeriodMetrics.From(
            activeEmployees: 5,
            participatingEmployees: 4,
            confirmedTransactions: 60,
            employeesWithBudget: 2,
            employeesWithActiveGoal: 1,
            goalsAchieved: 0,
            employeesWithIntegration: 1,
            employeesWithScore: 3,
            financialHealthScoreSum: 182);

        metrics.AverageFinancialHealthScore.ShouldBe(60.67m);
    }

    [Fact]
    public void Without_scored_employees_there_is_no_average_score()
    {
        PeriodMetrics.From(3, 3, 12, 0, 0, 0, 0, 0, 0).AverageFinancialHealthScore.ShouldBeNull();
    }

    [Fact]
    public void The_score_sum_cannot_exceed_one_hundred_per_scored_employee()
    {
        Should.NotThrow(() => PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 2, 200));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 2, 201));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 0, 10));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 1, -1));
    }

    [Fact]
    public void Employee_counts_cannot_exceed_the_active_employees()
    {
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 3, 0, 0, 0, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 3, 0, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 0, 3, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 0, 0, 0, 3, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(2, 0, 0, 0, 0, 0, 0, 3, 0));
    }

    [Fact]
    public void Negative_counts_are_rejected()
    {
        Should.Throw<DomainException>(() => PeriodMetrics.From(-1, 0, 0, 0, 0, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(1, 0, -1, 0, 0, 0, 0, 0, 0));
        Should.Throw<DomainException>(() => PeriodMetrics.From(1, 0, 0, 0, 0, -1, 0, 0, 0));
    }

    [Fact]
    public void Metrics_with_the_same_numbers_are_equal()
    {
        PeriodMetrics.From(3, 1, 4, 2, 0, 1, 3, 0, 0).ShouldBe(PeriodMetrics.From(3, 1, 4, 2, 0, 1, 3, 0, 0));
        PeriodMetrics.From(3, 1, 4, 2, 0, 1, 3, 0, 0).ShouldNotBe(PeriodMetrics.From(3, 2, 4, 2, 0, 1, 3, 0, 0));
    }
}
