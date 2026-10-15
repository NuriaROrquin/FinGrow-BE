namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class AchievementCatalogTests
{
    [Fact]
    public void Every_code_has_exactly_one_achievement_with_an_explicit_condition()
    {
        AchievementCatalog.All.Select(achievement => achievement.Code)
            .ShouldBe(Enum.GetValues<AchievementCode>(), ignoreOrder: true);

        AchievementCatalog.All.ShouldAllBe(achievement =>
            achievement.Name.Length > 0 && achievement.Description.Length > 0 && achievement.Condition.Length > 0);
    }

    [Fact]
    public void Only_monthly_close_achievements_are_left_to_the_job()
    {
        AchievementCatalog.InstantCodes.ShouldNotContain(AchievementCode.ConsistentThreeMonths);
        AchievementCatalog.InstantCodes.ShouldNotContain(AchievementCode.BudgetKept);
        AchievementCatalog.InstantCodes.ShouldNotContain(AchievementCode.Saver);
        AchievementCatalog.InstantCodes.Count.ShouldBe(AchievementCatalog.All.Count - 3);
    }

    [Fact]
    public void Finding_an_unknown_code_fails()
    {
        AchievementCatalog.Find(AchievementCode.Saver).Name.ShouldBe("Ahorrador");
        Should.Throw<DomainException>(() => AchievementCatalog.Find((AchievementCode)999));
    }

    [Theory]
    [InlineData(10, true, true)]
    [InlineData(9, true, false)]
    [InlineData(25, false, false)]
    public void An_active_month_needs_ten_confirmed_transactions_and_some_income(
        int confirmedTransactions,
        bool hasIncome,
        bool expected) =>
        AchievementCatalog.IsActiveMonth(confirmedTransactions, hasIncome).ShouldBe(expected);

    [Theory]
    [InlineData(100_000, 90_000, true)]
    [InlineData(100_000, 90_001, false)]
    [InlineData(100_000, 120_000, false)]
    public void Saving_means_keeping_at_least_ten_percent_of_income(decimal income, decimal spending, bool expected) =>
        AchievementCatalog.Saved(Money.From(income, Currency.ARS), Money.From(spending, Currency.ARS)).ShouldBe(expected);

    [Fact]
    public void A_month_without_income_does_not_count_as_saving() =>
        AchievementCatalog.Saved(Money.Zero(Currency.ARS), Money.Zero(Currency.ARS)).ShouldBeFalse();

    [Fact]
    public void A_budget_is_kept_when_no_category_went_over_its_limit()
    {
        AchievementCatalog.KeptBudget(new[] { BudgetHealth.OnTrack, BudgetHealth.Warning }).ShouldBeTrue();
        AchievementCatalog.KeptBudget(new[] { BudgetHealth.OnTrack, BudgetHealth.Exceeded }).ShouldBeFalse();
        AchievementCatalog.KeptBudget(Array.Empty<BudgetHealth>()).ShouldBeFalse();
    }
}
