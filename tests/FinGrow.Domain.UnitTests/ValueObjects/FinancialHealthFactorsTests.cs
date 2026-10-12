namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class FinancialHealthFactorsTests
{
    private static readonly DateTimeOffset GoalsCreatedAt = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly April = new(2026, 4, 1);

    [Theory]
    [InlineData(80000, 100)]
    [InlineData(90000, 50)]
    [InlineData(100000, 0)]
    [InlineData(130000, 0)]
    [InlineData(50000, 100)]
    public void Saving_a_fifth_of_the_income_is_the_full_savings_factor(decimal spending, decimal expected)
    {
        FinancialHealthFactors.SavingsOf(Ars(100000m), Ars(spending)).ShouldBe(expected);
    }

    [Fact]
    public void Savings_cannot_be_measured_without_income()
    {
        Should.Throw<DomainException>(() => FinancialHealthFactors.SavingsOf(Ars(0m), Ars(1000m)));
    }

    [Fact]
    public void The_budget_factor_is_the_share_of_categories_that_were_not_exceeded()
    {
        var categories = new[] { BudgetHealth.OnTrack, BudgetHealth.Warning, BudgetHealth.Exceeded, BudgetHealth.OnTrack };

        FinancialHealthFactors.BudgetOf(categories).ShouldBe(75m);
    }

    [Fact]
    public void Without_budget_limits_there_is_no_budget_factor()
    {
        FinancialHealthFactors.BudgetOf(Array.Empty<BudgetHealth>()).ShouldBeNull();
    }

    [Fact]
    public void The_goals_factor_averages_how_much_of_each_monthly_commitment_was_covered()
    {
        var half = NewGoal();
        half.AddContribution(Ars(50000m), new DateOnly(2026, 4, 10), null, GoalsCreatedAt);
        var full = NewGoal();
        full.AddContribution(Ars(100000m), new DateOnly(2026, 4, 20), null, GoalsCreatedAt);

        FinancialHealthFactors.GoalsOf(new[] { half, full }, April).ShouldBe(75m);
    }

    [Fact]
    public void Goals_without_a_commitment_that_month_do_not_count()
    {
        var cancelled = NewGoal();
        cancelled.Cancel(GoalsCreatedAt);

        FinancialHealthFactors.GoalsOf(new[] { cancelled }, April).ShouldBeNull();
        FinancialHealthFactors.GoalsOf(Array.Empty<Goal>(), April).ShouldBeNull();
    }

    [Theory]
    [InlineData(100000, 100)]
    [InlineData(200000, 50)]
    [InlineData(900000, 11.11)]
    public void Three_months_of_spending_saved_is_the_full_cushion(decimal averageMonthlySpending, decimal expected)
    {
        FinancialHealthFactors.CushionOf(Ars(1000000m), Ars(700000m), Ars(averageMonthlySpending)).ShouldBe(expected);
    }

    [Fact]
    public void A_negative_balance_is_no_cushion()
    {
        FinancialHealthFactors.CushionOf(Ars(500000m), Ars(700000m), Ars(100000m)).ShouldBe(0m);
    }

    [Fact]
    public void Without_spending_any_positive_balance_is_a_full_cushion()
    {
        FinancialHealthFactors.CushionOf(Ars(1000m), Ars(0m), Ars(0m)).ShouldBe(100m);
        FinancialHealthFactors.CushionOf(Ars(0m), Ars(0m), Ars(0m)).ShouldBe(0m);
    }

    [Fact]
    public void The_cushion_is_measured_in_a_single_currency()
    {
        Should.Throw<DomainException>(() =>
            FinancialHealthFactors.CushionOf(Ars(1000m), Ars(0m), Money.From(10m, Currency.USD)));
    }

    [Fact]
    public void The_score_weights_savings_budget_goals_and_cushion()
    {
        var factors = FinancialHealthFactors.From(savings: 50m, budget: 100m, goals: 0m, cushion: 100m);

        factors.Score.ShouldBe(60);
        factors.Band.ShouldBe(FinancialHealthBand.Stable);
    }

    [Fact]
    public void A_missing_factor_hands_its_weight_to_the_others()
    {
        FinancialHealthFactors.From(savings: 50m, budget: null, goals: null, cushion: 100m).Score.ShouldBe(64);
        FinancialHealthFactors.From(savings: 80m, budget: 60m, goals: null, cushion: 30m).Score.ShouldBe(64);
    }

    [Fact]
    public void The_score_rounds_half_points_up()
    {
        FinancialHealthFactors.From(savings: 1.25m, budget: 0m, goals: 0m, cushion: 0m).Score.ShouldBe(1);
    }

    [Fact]
    public void Every_factor_goes_from_zero_to_one_hundred()
    {
        Should.Throw<DomainException>(() => FinancialHealthFactors.From(-1m, null, null, 0m));
        Should.Throw<DomainException>(() => FinancialHealthFactors.From(0m, 101m, null, 0m));
        Should.Throw<DomainException>(() => FinancialHealthFactors.From(0m, null, 100.01m, 0m));
        Should.Throw<DomainException>(() => FinancialHealthFactors.From(0m, null, null, 120m));
    }

    [Theory]
    [InlineData(9, 1000, false)]
    [InlineData(10, 1000, true)]
    [InlineData(25, 0, false)]
    public void Only_a_month_with_ten_confirmed_transactions_and_some_income_gets_a_score(
        int confirmedTransactions,
        decimal income,
        bool expected)
    {
        FinancialHealthFactors.HasEnoughActivity(confirmedTransactions, Ars(income)).ShouldBe(expected);
    }

    [Fact]
    public void Factors_with_the_same_values_are_equal()
    {
        FinancialHealthFactors.From(50m, null, 20m, 10m).ShouldBe(FinancialHealthFactors.From(50m, null, 20m, 10m));
        FinancialHealthFactors.From(50m, null, 20m, 10m).ShouldNotBe(FinancialHealthFactors.From(50m, 0m, 20m, 10m));
    }

    private static Money Ars(decimal amount) => Money.From(amount, Currency.ARS);

    private static Goal NewGoal() => Goal.Create(
        Guid.CreateVersion7(),
        "Vacaciones",
        Ars(1000000m),
        new DateOnly(2026, 12, 31),
        GoalsCreatedAt);
}
