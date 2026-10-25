namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class CompanyConcernsCalculatorTests
{
    private static readonly decimal[] PercentagesByWeight = { 100m, 75m, 50m, 33.33m };

    [Fact]
    public void A_factor_below_half_is_a_concern_and_exactly_half_is_not()
    {
        var concerns = CompanyConcernsCalculator.Count(new[]
        {
            Factors(savings: 49.99m),
            Factors(savings: 50m),
        });

        var savings = concerns.ShouldHaveSingleItem();
        savings.Concern.ShouldBe(CompanyConcern.LowSavings);
        savings.AffectedEmployees.ShouldBe(1);
        savings.EvaluatedEmployees.ShouldBe(2);
        savings.Percentage.ShouldBe(50m);
    }

    [Fact]
    public void Budget_and_goals_are_measured_only_among_employees_that_have_them()
    {
        var concerns = CompanyConcernsCalculator.Count(new[]
        {
            Factors(budget: 0m, goals: 100m),
            Factors(budget: 100m),
            Factors(),
            Factors(),
        });

        var budget = concerns.ShouldHaveSingleItem();
        budget.Concern.ShouldBe(CompanyConcern.BudgetOverruns);
        budget.AffectedEmployees.ShouldBe(1);
        budget.EvaluatedEmployees.ShouldBe(2);
        budget.Percentage.ShouldBe(50m);
    }

    [Fact]
    public void Concerns_are_ordered_by_percentage_then_by_affected_employees()
    {
        var concerns = CompanyConcernsCalculator.Count(new[]
        {
            Factors(savings: 10m, cushion: 10m, goals: 10m),
            Factors(savings: 10m, cushion: 10m, budget: 10m),
            Factors(savings: 10m, budget: 90m),
            Factors(budget: 90m),
        });

        concerns.Select(concern => concern.Concern).ShouldBe(new[]
        {
            CompanyConcern.MissedGoalContributions,
            CompanyConcern.LowSavings,
            CompanyConcern.ThinCushion,
            CompanyConcern.BudgetOverruns,
        });
        concerns.Select(concern => concern.Percentage).ShouldBe(PercentagesByWeight);
    }

    [Fact]
    public void A_concern_with_the_same_weight_keeps_the_catalog_order()
    {
        var concerns = CompanyConcernsCalculator.Count(new[]
        {
            Factors(savings: 10m, cushion: 10m),
            Factors(),
        });

        concerns.Select(concern => concern.Concern).ShouldBe(new[]
        {
            CompanyConcern.LowSavings,
            CompanyConcern.ThinCushion,
        });
    }

    [Fact]
    public void Without_anyone_below_half_there_are_no_concerns()
    {
        CompanyConcernsCalculator.Count(new[] { Factors(), Factors(budget: 80m, goals: 60m) }).ShouldBeEmpty();
    }

    [Fact]
    public void Without_scores_there_are_no_concerns()
    {
        CompanyConcernsCalculator.Count(Array.Empty<FinancialHealthFactors>()).ShouldBeEmpty();
    }

    private static FinancialHealthFactors Factors(
        decimal savings = 100m,
        decimal? budget = null,
        decimal? goals = null,
        decimal cushion = 100m) =>
        FinancialHealthFactors.From(savings, budget, goals, cushion);
}
