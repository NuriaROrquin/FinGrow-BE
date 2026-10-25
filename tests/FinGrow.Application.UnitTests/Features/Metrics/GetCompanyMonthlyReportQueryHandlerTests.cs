namespace FinGrow.Application.UnitTests.Features.Metrics;

using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyMonthlyReport;
using FinGrow.Application.Features.Metrics.MonthlyReport;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetCompanyMonthlyReportQueryHandlerTests
{
    private static readonly MetricsPeriod July = MetricsPeriod.Of(2026, 7);
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);

    private static readonly CompanyTrendIndicator[] ExpectedPositiveTrends =
    {
        CompanyTrendIndicator.ParticipationRate,
        CompanyTrendIndicator.LowSavings,
        CompanyTrendIndicator.AverageFinancialHealthScore,
        CompanyTrendIndicator.CompletedCourses,
        CompanyTrendIndicator.GoalsAchieved,
    };

    private static readonly CompanyTrendIndicator[] ExpectedOpportunities = { CompanyTrendIndicator.GoalAdoption };

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();
    private readonly FakeFinancialHealthScoreRepository _scores = new();
    private readonly FakeCompanyEducationMetricsReadRepository _education = new();

    private static MetricsAnonymizationPolicy Policy =>
        new(Options.Create(new MetricsAnonymizationOptions()));

    private CompanyMonthlyReportBuilder CreateBuilder() => new(_snapshots, _scores, _education, Policy);

    private GetCompanyMonthlyReportQueryHandler CreateHandler() =>
        new(_scenario.CurrentUser, _scenario.Companies, _snapshots, CreateBuilder(), Policy);

    [Fact]
    public async Task Reports_the_last_closed_month_against_the_previous_one()
    {
        TakeSnapshot(August, PeriodMetrics.From(10, 5, 100, 2, 4, 1, 3, 4, 240));
        AddScores(August, LowSavings(), LowSavings(), LowSavings(), Healthy());
        _education.CompletedCourses[(_scenario.Company.Id, August.Start)] = 2;
        TakeSnapshot(September, PeriodMetrics.From(10, 8, 120, 2, 3, 4, 3, 5, 350));
        AddScores(September, LowSavings(), LowSavings(), LowSavings(), Healthy(), Healthy());
        _education.CompletedCourses[(_scenario.Company.Id, September.Start)] = 6;

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value;
        report.CompanyName.ShouldBe("Acme S.A.");
        report.Period.ShouldBe(September.Start);
        report.PreviousPeriod.ShouldBe(August.Start);
        report.MinimumGroupSize.ShouldBe(3);
        report.IsAnonymized.ShouldBeFalse();
        report.ActiveEmployees.ShouldBe(10);
        report.ParticipatingEmployees.ShouldBe(8);
        report.AverageScoreBand.ShouldBe(FinancialHealthBand.Stable);
        report.Indicators
            .Select(indicator => (indicator.Indicator, indicator.Unit, indicator.Status, indicator.Value, indicator.PreviousStatus, indicator.PreviousValue, indicator.Change))
            .ShouldBe(new (CompanyTrendIndicator, CompanyTrendUnit, CompanyReportValueStatus, decimal?, CompanyReportValueStatus, decimal?, decimal?)[]
            {
                (CompanyTrendIndicator.ParticipationRate, CompanyTrendUnit.Percentage, CompanyReportValueStatus.Visible, 80m, CompanyReportValueStatus.Visible, 50m, 30m),
                (CompanyTrendIndicator.AverageFinancialHealthScore, CompanyTrendUnit.Points, CompanyReportValueStatus.Visible, 70m, CompanyReportValueStatus.Visible, 60m, 10m),
                (CompanyTrendIndicator.CompletedCourses, CompanyTrendUnit.Count, CompanyReportValueStatus.Visible, 6m, CompanyReportValueStatus.Visible, 2m, 4m),
            });
        report.Trends.Period.ShouldBe(September.Start);
        report.Trends.PreviousPeriod.ShouldBe(August.Start);
        report.Trends.IsAnonymized.ShouldBeFalse();
        report.Trends.PositiveTrends.Select(trend => trend.Indicator).ShouldBe(ExpectedPositiveTrends);
        report.Trends.Opportunities.Select(trend => trend.Indicator).ShouldBe(ExpectedOpportunities);
    }

    [Fact]
    public async Task Without_any_photographed_month_there_is_no_period_but_there_is_a_company_name()
    {
        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.CompanyName.ShouldBe("Acme S.A.");
        result.Value.Period.ShouldBeNull();
        result.Value.PreviousPeriod.ShouldBeNull();
        result.Value.Indicators.ShouldBeEmpty();
        result.Value.Trends.Period.ShouldBeNull();
    }

    [Fact]
    public async Task Without_the_photo_of_the_month_right_before_there_is_nothing_to_compare()
    {
        TakeSnapshot(July, PeriodMetrics.From(5, 1, 50, 0, 0, 0, 0, 0, 0));
        TakeSnapshot(September, PeriodMetrics.From(5, 4, 50, 0, 0, 0, 0, 3, 210));

        var result = await Handle();

        var report = result.Value;
        report.Period.ShouldBe(September.Start);
        report.PreviousPeriod.ShouldBe(August.Start);
        report.Indicators.ShouldAllBe(indicator =>
            indicator.Status == CompanyReportValueStatus.Visible
            && indicator.PreviousStatus == CompanyReportValueStatus.NoData
            && indicator.PreviousValue == null
            && indicator.Change == null);
        report.Trends.PreviousPeriod.ShouldBeNull();
        report.Trends.PositiveTrends.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_month_with_fewer_active_employees_than_the_threshold_hides_everything_and_says_why()
    {
        TakeSnapshot(August, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 3, 210));
        TakeSnapshot(September, PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 2, 140));
        _education.CompletedCourses[(_scenario.Company.Id, September.Start)] = 1;

        var result = await Handle();

        var report = result.Value;
        report.IsAnonymized.ShouldBeTrue();
        report.ActiveEmployees.ShouldBe(2);
        report.ParticipatingEmployees.ShouldBeNull();
        report.AverageScoreBand.ShouldBeNull();
        report.Indicators.Count.ShouldBe(3);
        report.Indicators.ShouldAllBe(indicator =>
            indicator.Status == CompanyReportValueStatus.TooFewActiveEmployees
            && indicator.Value == null
            && indicator.Change == null);
        report.Trends.IsAnonymized.ShouldBeTrue();
        report.Trends.PositiveTrends.ShouldBeEmpty();
        report.Trends.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_small_previous_month_shows_the_current_one_without_comparison()
    {
        TakeSnapshot(August, PeriodMetrics.From(2, 1, 20, 0, 0, 0, 0, 0, 0));
        TakeSnapshot(September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 0, 0));

        var result = await Handle();

        var report = result.Value;
        report.IsAnonymized.ShouldBeFalse();
        var participation = report.Indicators.Single(indicator => indicator.Indicator == CompanyTrendIndicator.ParticipationRate);
        participation.Status.ShouldBe(CompanyReportValueStatus.Visible);
        participation.Value.ShouldBe(100m);
        participation.PreviousStatus.ShouldBe(CompanyReportValueStatus.TooFewActiveEmployees);
        participation.PreviousValue.ShouldBeNull();
        participation.Change.ShouldBeNull();
        report.Trends.IsAnonymized.ShouldBeTrue();
    }

    [Fact]
    public async Task Few_employees_with_a_score_hide_only_the_average_score()
    {
        TakeSnapshot(August, PeriodMetrics.From(10, 6, 100, 0, 0, 0, 0, 4, 200));
        TakeSnapshot(September, PeriodMetrics.From(10, 7, 100, 0, 0, 0, 0, 2, 180));

        var result = await Handle();

        var report = result.Value;
        report.AverageScoreBand.ShouldBeNull();
        var score = report.Indicators.Single(indicator => indicator.Indicator == CompanyTrendIndicator.AverageFinancialHealthScore);
        score.Status.ShouldBe(CompanyReportValueStatus.TooFewScoredEmployees);
        score.Value.ShouldBeNull();
        score.PreviousStatus.ShouldBe(CompanyReportValueStatus.Visible);
        score.PreviousValue.ShouldBe(50m);
        score.Change.ShouldBeNull();
        report.Indicators
            .Single(indicator => indicator.Indicator == CompanyTrendIndicator.ParticipationRate)
            .Status.ShouldBe(CompanyReportValueStatus.Visible);
    }

    [Fact]
    public async Task The_report_carries_no_employee_identifier()
    {
        var ana = _scenario.AddEmployee(department: null, email: "ana.gomez@acme.com", fullName: "Ana Gomez");
        TakeSnapshot(August, PeriodMetrics.From(4, 2, 40, 0, 0, 0, 0, 4, 200));
        var employees = AddScores(August, LowSavings(), LowSavings(), LowSavings(), Healthy());
        TakeSnapshot(September, PeriodMetrics.From(4, 4, 40, 0, 0, 0, 0, 4, 400));
        employees.AddRange(AddScores(September, Healthy(), Healthy(), Healthy(), Healthy()));

        var result = await Handle();

        var response = JsonSerializer.Serialize(result.Value);
        result.Value.Trends.PositiveTrends.ShouldNotBeEmpty();
        foreach (var employeeId in employees.Append(ana.Id))
        {
            response.ShouldNotContain(employeeId.ToString());
        }

        response.ShouldNotContain("Ana Gomez");
        response.ShouldNotContain("ana.gomez@acme.com");
        response.ShouldNotContain(_scenario.Company.Id.ToString());
    }

    [Fact]
    public async Task Only_reads_the_photos_of_its_own_company()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "rrhh@otra.com");
        _snapshots.Add(CompanyMetricsSnapshot.Take(other.Id, August, PeriodMetrics.From(5, 1, 50, 0, 0, 0, 0, 0, 0), CompanyStaffScenario.Now));
        _snapshots.Add(CompanyMetricsSnapshot.Take(other.Id, September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 0, 0), CompanyStaffScenario.Now));

        var result = await Handle();

        result.Value.CompanyName.ShouldBe("Acme S.A.");
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

    [Fact]
    public async Task The_builder_reports_any_requested_month_and_not_only_the_last_one()
    {
        TakeSnapshot(July, PeriodMetrics.From(5, 2, 50, 0, 0, 0, 0, 0, 0));
        TakeSnapshot(August, PeriodMetrics.From(5, 4, 50, 0, 0, 0, 0, 0, 0));
        TakeSnapshot(September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 0, 0));

        var built = await CreateBuilder().BuildAsync(_scenario.Company, August, CancellationToken.None);

        var report = built.ShouldNotBeNull();
        report.Period.ShouldBe(August.Start);
        report.PreviousPeriod.ShouldBe(July.Start);
        var participation = report.Indicators.Single(indicator => indicator.Indicator == CompanyTrendIndicator.ParticipationRate);
        participation.Value.ShouldBe(80m);
        participation.PreviousValue.ShouldBe(40m);
    }

    [Fact]
    public async Task The_builder_returns_nothing_for_a_month_without_photo()
    {
        TakeSnapshot(August, PeriodMetrics.From(5, 4, 50, 0, 0, 0, 0, 0, 0));

        var report = await CreateBuilder().BuildAsync(_scenario.Company, September, CancellationToken.None);

        report.ShouldBeNull();
    }

    private Task<Result<CompanyMonthlyReportResponse>> Handle() =>
        CreateHandler().Handle(new GetCompanyMonthlyReportQuery(), CancellationToken.None);

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
}
