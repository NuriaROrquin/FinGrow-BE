namespace FinGrow.Application.UnitTests.Features.Metrics;

using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyTrends;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetCompanyTrendsQueryHandlerTests
{
    private static readonly MetricsPeriod July = MetricsPeriod.Of(2026, 7);
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();
    private readonly FakeFinancialHealthScoreRepository _scores = new();
    private readonly FakeCompanyEducationMetricsReadRepository _education = new();

    private GetCompanyTrendsQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            _scores,
            _education,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())));

    [Fact]
    public async Task Compares_the_last_closed_month_with_the_previous_one()
    {
        TakeSnapshot(August, PeriodMetrics.From(10, 5, 100, 2, 4, 1, 3, 4, 240));
        AddScores(August, LowSavings(), LowSavings(), LowSavings(), Healthy());
        _education.CompletedCourses[(_scenario.Company.Id, August.Start)] = 2;
        TakeSnapshot(September, PeriodMetrics.From(10, 8, 120, 2, 3, 4, 3, 5, 350));
        AddScores(September, LowSavingsAndThinCushion(), LowSavingsAndThinCushion(), LowSavingsAndThinCushion(), Healthy(), Healthy());
        _education.CompletedCourses[(_scenario.Company.Id, September.Start)] = 6;

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBe(September.Start);
        result.Value.PreviousPeriod.ShouldBe(August.Start);
        result.Value.IsAnonymized.ShouldBeFalse();
        result.Value.PositiveTrends
            .Select(trend => (trend.Indicator, trend.PreviousValue, trend.CurrentValue, trend.Change))
            .ShouldBe(new[]
            {
                (CompanyTrendIndicator.ParticipationRate, 50m, 80m, 30m),
                (CompanyTrendIndicator.LowSavings, 75m, 60m, -15m),
                (CompanyTrendIndicator.AverageFinancialHealthScore, 60m, 70m, 10m),
                (CompanyTrendIndicator.CompletedCourses, 2m, 6m, 4m),
                (CompanyTrendIndicator.GoalsAchieved, 1m, 4m, 3m),
            });
        result.Value.Opportunities
            .Select(trend => (trend.Indicator, trend.PreviousValue, trend.CurrentValue, trend.Change))
            .ShouldBe(new[]
            {
                (CompanyTrendIndicator.ThinCushion, 0m, 60m, 60m),
                (CompanyTrendIndicator.GoalAdoption, 40m, 30m, -10m),
            });
    }

    [Fact]
    public async Task Without_any_photographed_month_there_is_no_period()
    {
        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBeNull();
        result.Value.PreviousPeriod.ShouldBeNull();
        result.Value.PositiveTrends.ShouldBeEmpty();
        result.Value.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public async Task With_a_single_month_there_is_nothing_to_compare()
    {
        TakeSnapshot(September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 0, 0));

        var result = await Handle();

        result.Value.Period.ShouldBe(September.Start);
        result.Value.PreviousPeriod.ShouldBeNull();
        result.Value.IsAnonymized.ShouldBeFalse();
        result.Value.PositiveTrends.ShouldBeEmpty();
        result.Value.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public async Task Only_compares_with_the_month_right_before()
    {
        TakeSnapshot(July, PeriodMetrics.From(5, 1, 50, 0, 0, 0, 0, 0, 0));
        TakeSnapshot(September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 0, 0));

        var result = await Handle();

        result.Value.Period.ShouldBe(September.Start);
        result.Value.PreviousPeriod.ShouldBeNull();
        result.Value.PositiveTrends.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_either_month_has_fewer_active_employees_than_the_threshold_nothing_is_shown()
    {
        TakeSnapshot(August, PeriodMetrics.From(2, 1, 20, 0, 0, 0, 0, 0, 0));
        TakeSnapshot(September, PeriodMetrics.From(5, 5, 50, 5, 5, 3, 5, 0, 0));

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBe(September.Start);
        result.Value.PreviousPeriod.ShouldBe(August.Start);
        result.Value.IsAnonymized.ShouldBeTrue();
        result.Value.MinimumGroupSize.ShouldBe(3);
        result.Value.PositiveTrends.ShouldBeEmpty();
        result.Value.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public async Task Small_changes_are_not_reported()
    {
        TakeSnapshot(August, PeriodMetrics.From(100, 50, 100, 20, 20, 1, 20, 0, 0));
        TakeSnapshot(September, PeriodMetrics.From(100, 51, 100, 21, 19, 2, 20, 0, 0));

        var result = await Handle();

        result.Value.IsAnonymized.ShouldBeFalse();
        result.Value.PositiveTrends.ShouldBeEmpty();
        result.Value.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_response_carries_no_employee_identifier()
    {
        TakeSnapshot(August, PeriodMetrics.From(4, 2, 40, 0, 0, 0, 0, 4, 200));
        var employees = AddScores(August, LowSavings(), LowSavings(), LowSavings(), Healthy());
        TakeSnapshot(September, PeriodMetrics.From(4, 4, 40, 0, 0, 0, 0, 4, 400));
        employees.AddRange(AddScores(September, Healthy(), Healthy(), Healthy(), Healthy()));

        var result = await Handle();

        var response = JsonSerializer.Serialize(result.Value);
        result.Value.PositiveTrends.ShouldNotBeEmpty();
        foreach (var employeeId in employees)
        {
            response.ShouldNotContain(employeeId.ToString());
        }

        response.ShouldNotContain(_scenario.Company.Id.ToString());
    }

    [Fact]
    public async Task Only_reads_the_photos_of_its_own_company()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "rrhh@otra.com");
        _snapshots.Add(CompanyMetricsSnapshot.Take(other.Id, August, PeriodMetrics.From(5, 1, 50, 0, 0, 0, 0, 0, 0), CompanyStaffScenario.Now));
        _snapshots.Add(CompanyMetricsSnapshot.Take(other.Id, September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 0, 0), CompanyStaffScenario.Now));

        var result = await Handle();

        result.Value.Period.ShouldBeNull();
    }

    [Fact]
    public async Task A_session_without_company_is_forbidden()
    {
        _scenario.LogOut();

        var result = await Handle();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private Task<Result<CompanyTrendsResponse>> Handle() =>
        CreateHandler().Handle(new GetCompanyTrendsQuery(), CancellationToken.None);

    private void TakeSnapshot(MetricsPeriod period, PeriodMetrics metrics) =>
        _snapshots.Add(CompanyMetricsSnapshot.Take(_scenario.Company.Id, period, metrics, CompanyStaffScenario.Now));

    private List<Guid> AddScores(MetricsPeriod period, params FinancialHealthFactors[] factors)
    {
        var employeeIds = new List<Guid>();

        foreach (var factor in factors)
        {
            var employeeId = Guid.CreateVersion7();
            employeeIds.Add(employeeId);
            _scores.Add(FinancialHealthScore.Compute(employeeId, period, factor, CompanyStaffScenario.Now));
        }

        return employeeIds;
    }

    private static FinancialHealthFactors Healthy() => FinancialHealthFactors.From(100m, null, null, 100m);

    private static FinancialHealthFactors LowSavings() => FinancialHealthFactors.From(20m, null, null, 100m);

    private static FinancialHealthFactors LowSavingsAndThinCushion() => FinancialHealthFactors.From(20m, null, null, 20m);
}
