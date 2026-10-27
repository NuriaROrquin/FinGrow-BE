namespace FinGrow.Application.UnitTests.Features.Metrics;

using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class GetCompanyInsightsQueryHandlerTests
{
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();
    private readonly FakeFinancialHealthScoreRepository _scores = new();
    private readonly FakeAiService _ai = new();

    private static MetricsAnonymizationPolicy Policy =>
        new(Options.Create(new MetricsAnonymizationOptions()));

    private GetCompanyInsightsQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            Policy,
            new CompanyInsightsBuilder(_snapshots, _scores, Policy, _ai, NullLogger<CompanyInsightsBuilder>.Instance));

    [Fact]
    public async Task Returns_the_concerns_of_the_last_photographed_month_ordered_by_weight()
    {
        TakeSnapshot(August, PeriodMetrics.From(6, 6, 60, 0, 0, 0, 0, 3, 300));
        AddScores(August, LowSavings(), LowSavings(), LowSavings());
        TakeSnapshot(September, PeriodMetrics.From(8, 6, 80, 4, 0, 0, 0, 6, 300));
        AddScores(
            September,
            LowSavingsAndThinCushion(budget: 10m),
            LowSavingsAndThinCushion(budget: 10m),
            LowSavingsAndThinCushion(budget: 10m),
            LowSavings(budget: 100m),
            Healthy(),
            Healthy());

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBe(September.Start);
        result.Value.IsAnonymized.ShouldBeFalse();
        result.Value.MeasuredEmployees.ShouldBe(6);
        result.Value.Concerns
            .Select(concern => (concern.Concern, concern.AffectedEmployees, concern.EvaluatedEmployees, concern.Percentage))
            .ShouldBe(new[]
            {
                (CompanyConcern.BudgetOverruns, 3, 4, 75m),
                (CompanyConcern.LowSavings, 4, 6, 66.67m),
                (CompanyConcern.ThinCushion, 3, 6, 50m),
            });
    }

    [Fact]
    public async Task Hides_a_concern_that_affects_fewer_employees_than_the_threshold()
    {
        TakeSnapshot(September, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 5, 300));
        AddScores(September, LowSavingsAndThinCushion(), LowSavingsAndThinCushion(), LowSavings(), Healthy(), Healthy());

        var result = await Handle();

        var concern = result.Value.Concerns.ShouldHaveSingleItem();
        concern.Concern.ShouldBe(CompanyConcern.LowSavings);
        _ai.CompanyRecommendationRequests.Single().Concerns.Select(summary => summary.Concern)
            .ShouldBe(new[] { CompanyConcern.LowSavings });
    }

    [Fact]
    public async Task With_fewer_measured_employees_than_the_threshold_nothing_is_shown_nor_sent_to_the_ai()
    {
        TakeSnapshot(September, PeriodMetrics.From(10, 10, 100, 0, 0, 0, 0, 2, 40));
        AddScores(September, LowSavings(), LowSavings());

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBe(September.Start);
        result.Value.IsAnonymized.ShouldBeTrue();
        result.Value.MinimumGroupSize.ShouldBe(3);
        result.Value.MeasuredEmployees.ShouldBeNull();
        result.Value.Concerns.ShouldBeEmpty();
        result.Value.Recommendations.ShouldBeEmpty();
        result.Value.RecommendationsAvailable.ShouldBeFalse();
        _ai.CompanyRecommendationRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_any_photographed_month_there_is_no_period()
    {
        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Period.ShouldBeNull();
        result.Value.IsAnonymized.ShouldBeFalse();
        result.Value.Concerns.ShouldBeEmpty();
        result.Value.RecommendationsAvailable.ShouldBeFalse();
        _ai.CompanyRecommendationRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_concerns_the_ai_still_recommends()
    {
        TakeSnapshot(September, PeriodMetrics.From(3, 3, 30, 0, 0, 0, 0, 3, 300));
        AddScores(September, Healthy(), Healthy(), Healthy());

        var result = await Handle();

        result.Value.Concerns.ShouldBeEmpty();
        result.Value.RecommendationsAvailable.ShouldBeTrue();
        result.Value.Recommendations.Count.ShouldBe(3);
        _ai.CompanyRecommendationRequests.Single().Concerns.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_ai_is_down_the_concerns_are_shown_without_recommendations()
    {
        TakeSnapshot(September, PeriodMetrics.From(3, 3, 30, 0, 0, 0, 0, 3, 60));
        AddScores(September, LowSavings(), LowSavings(), LowSavings());
        _ai.Unreachable = true;

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Concerns.ShouldHaveSingleItem().Concern.ShouldBe(CompanyConcern.LowSavings);
        result.Value.Recommendations.ShouldBeEmpty();
        result.Value.RecommendationsAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task The_ai_receives_the_month_the_company_language_and_the_revealed_indicators()
    {
        _scenario.Company.UpdatePreferences(
            _scenario.Company.Theme,
            Language.en,
            _scenario.Company.DefaultCurrency,
            _scenario.Company.FiscalYearStartMonth,
            CompanyStaffScenario.Now);
        TakeSnapshot(September, PeriodMetrics.From(4, 3, 40, 0, 0, 0, 0, 3, 180));
        AddScores(September, LowSavings(), LowSavings(), LowSavings());

        await Handle();

        var input = _ai.CompanyRecommendationRequests.Single();
        input.Language.ShouldBe(Language.en);
        (input.Year, input.Month).ShouldBe((2026, 9));
        input.MeasuredEmployees.ShouldBe(3);
        input.AverageScore.ShouldBe(60m);
        input.ParticipationRate.ShouldBe(75m);
    }

    [Fact]
    public async Task The_data_sent_to_the_ai_and_the_response_carry_no_employee_identifier()
    {
        TakeSnapshot(September, PeriodMetrics.From(4, 4, 40, 0, 0, 0, 0, 4, 120));
        var employees = AddScores(September, LowSavings(), LowSavings(), LowSavings(), Healthy());

        var result = await Handle();

        var sentToAi = JsonSerializer.Serialize(_ai.CompanyRecommendationRequests.Single());
        var response = JsonSerializer.Serialize(result.Value);
        foreach (var employeeId in employees)
        {
            sentToAi.ShouldNotContain(employeeId.ToString());
            response.ShouldNotContain(employeeId.ToString());
        }

        sentToAi.ShouldNotContain(_scenario.Company.Id.ToString());
        response.ShouldNotContain(_scenario.Company.Id.ToString());
    }

    [Fact]
    public async Task Only_reads_the_photos_and_scores_of_its_own_company()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "rrhh@otra.com");
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            other.Id,
            September,
            PeriodMetrics.From(3, 3, 30, 0, 0, 0, 0, 3, 60),
            CompanyStaffScenario.Now));
        foreach (var employeeId in AddScores(September, LowSavings(), LowSavings(), LowSavings()))
        {
            _scores.CompanyByEmployee[employeeId] = other.Id;
        }

        var result = await Handle();

        result.Value.Period.ShouldBeNull();

        TakeSnapshot(September, PeriodMetrics.From(3, 3, 30, 0, 0, 0, 0, 0, 0));

        var ownMonth = await Handle();

        ownMonth.Value.Period.ShouldBe(September.Start);
        ownMonth.Value.IsAnonymized.ShouldBeTrue();
    }

    [Fact]
    public async Task A_session_without_company_is_forbidden()
    {
        _scenario.LogOut();

        var result = await Handle();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private Task<Result<CompanyInsightsResponse>> Handle() =>
        CreateHandler().Handle(new GetCompanyInsightsQuery(), CancellationToken.None);

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

    private static FinancialHealthFactors LowSavings(decimal? budget = null) =>
        FinancialHealthFactors.From(20m, budget, null, 100m);

    private static FinancialHealthFactors LowSavingsAndThinCushion(decimal? budget = null) =>
        FinancialHealthFactors.From(20m, budget, null, 20m);
}
