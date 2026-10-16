namespace FinGrow.Application.UnitTests.Features.Reports.ExpenseBreakdown;

using System.Globalization;
using FinGrow.Application.Features.Reports.ExpenseBreakdown;
using FinGrow.Application.Features.Reports.MonthlySummary;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Application.Validations.Reports;
using FinGrow.Domain.Enums;

public class GetExpenseBreakdownHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly September = new(2026, 9, 1);
    private static readonly DateOnly EndOfSeptember = new(2026, 9, 30);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeMonthlySummaryReadRepository _movements = new();

    private async Task<ExpenseBreakdownResponse> RunAsync(DateOnly from, DateOnly to, Currency currency = Currency.ARS) =>
        (await new GetExpenseBreakdownHandler(_movements)
            .Handle(new GetExpenseBreakdownQuery(_employeeId, currency, from, to), CancellationToken.None)).Value;

    private Task<ExpenseBreakdownResponse> RunSeptemberAsync() => RunAsync(September, EndOfSeptember);

    [Fact]
    public async Task Categories_are_sorted_from_highest_to_lowest_with_their_share_of_consumption()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 6), 600m, ExpenseCategory.Vivienda);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 7), 100m, ExpenseCategory.Transporte);

        var breakdown = await RunSeptemberAsync();

        breakdown.Categories.Select(category => (category.Category, category.Amount, category.Percentage)).ShouldBe(new[]
        {
            (ExpenseCategory.Vivienda, 600m, 60.0m),
            (ExpenseCategory.Alimentos, 300m, 30.0m),
            (ExpenseCategory.Transporte, 100m, 10.0m),
        });
    }

    [Fact]
    public async Task Percentages_are_rounded_to_one_decimal()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 100m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 6), 200m, ExpenseCategory.Vivienda);

        var breakdown = await RunSeptemberAsync();

        breakdown.Categories[0].Percentage.ShouldBe(66.7m);
        breakdown.Categories[1].Percentage.ShouldBe(33.3m);
    }

    [Fact]
    public async Task Ahorro_inversion_is_left_out_of_the_ranking_and_of_the_base_of_the_percentages()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 400m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 6), 600m, ExpenseCategory.AhorroInversion);

        var breakdown = await RunSeptemberAsync();

        var category = breakdown.Categories.ShouldHaveSingleItem();
        category.Category.ShouldBe(ExpenseCategory.Alimentos);
        category.Percentage.ShouldBe(100.0m);
        breakdown.Consumption.Amount.ShouldBe(400m);
        breakdown.Savings.Amount.ShouldBe(600m);
        breakdown.Total.Amount.ShouldBe(1000m);
    }

    [Fact]
    public async Task The_total_matches_the_total_expenses_of_the_monthly_summary()
    {
        _movements.Income(_employeeId, new DateOnly(2026, 8, 1), 5000m);
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 3), 700m, ExpenseCategory.Servicios);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 4), 1200m, ExpenseCategory.Vivienda);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.AhorroInversion);
        var from = new DateOnly(2026, 8, 1);

        var breakdown = await RunAsync(from, EndOfSeptember);
        var summary = (await new GetMonthlySummaryHandler(_movements, new FakeDateTimeProvider(Now))
            .Handle(new GetMonthlySummaryQuery(_employeeId, Currency.ARS, from, EndOfSeptember), CancellationToken.None)).Value;

        breakdown.Total.Amount.ShouldBe(summary.TotalExpenses);
        (breakdown.Consumption.Amount + breakdown.Savings.Amount).ShouldBe(breakdown.Total.Amount);
    }

    [Theory]
    [InlineData("2026-09-01", "2026-09-30", "2026-08-01", "2026-08-31")]
    [InlineData("2026-08-01", "2026-09-30", "2026-06-01", "2026-07-31")]
    [InlineData("2026-05-01", "2026-10-16", "2025-11-01", "2026-04-16")]
    [InlineData("2026-03-01", "2026-03-31", "2026-02-01", "2026-02-28")]
    public async Task The_previous_period_has_the_same_number_of_months_right_before(
        string from,
        string to,
        string previousFrom,
        string previousTo)
    {
        var breakdown = await RunAsync(Parse(from), Parse(to));

        breakdown.PreviousFrom.ShouldBe(Parse(previousFrom));
        breakdown.PreviousTo.ShouldBe(Parse(previousTo));
    }

    [Fact]
    public async Task Each_category_shows_its_variation_against_the_previous_period()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 5), 200m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 6), 400m, ExpenseCategory.Vivienda);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 6), 300m, ExpenseCategory.Vivienda);

        var breakdown = await RunSeptemberAsync();

        breakdown.HasPreviousPeriod.ShouldBeTrue();
        breakdown.Categories.Single(category => category.Category == ExpenseCategory.Alimentos).Comparison
            .ShouldBe(new ExpenseComparison(200m, 100m, 50.0m, false));
        breakdown.Categories.Single(category => category.Category == ExpenseCategory.Vivienda).Comparison
            .ShouldBe(new ExpenseComparison(400m, -100m, -25.0m, false));
        breakdown.Consumption.Comparison.ShouldBe(new ExpenseComparison(600m, 0m, 0.0m, false));
    }

    [Fact]
    public async Task A_category_without_spending_in_the_previous_period_is_new()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 5), 200m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 50m, ExpenseCategory.Salud);

        var breakdown = await RunSeptemberAsync();

        breakdown.Categories.Single(category => category.Category == ExpenseCategory.Salud).Comparison
            .ShouldBe(new ExpenseComparison(0m, 50m, null, true));
    }

    [Fact]
    public async Task A_category_that_stopped_having_spending_drops_one_hundred_percent_at_the_end_of_the_list()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 5), 80m, ExpenseCategory.Transporte);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.Alimentos);

        var breakdown = await RunSeptemberAsync();

        var transport = breakdown.Categories[^1];
        transport.Category.ShouldBe(ExpenseCategory.Transporte);
        transport.Amount.ShouldBe(0m);
        transport.Percentage.ShouldBe(0m);
        transport.Comparison.ShouldBe(new ExpenseComparison(80m, -80m, -100.0m, false));
    }

    [Fact]
    public async Task Without_history_before_the_previous_period_ends_there_is_no_comparison()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 5), 99m, ExpenseCategory.Alimentos, Currency.USD);

        var breakdown = await RunSeptemberAsync();

        breakdown.HasPreviousPeriod.ShouldBeFalse();
        breakdown.Categories.ShouldAllBe(category => category.Comparison == null);
        breakdown.Consumption.Comparison.ShouldBeNull();
        breakdown.Savings.Comparison.ShouldBeNull();
        breakdown.Total.Comparison.ShouldBeNull();
    }

    [Fact]
    public async Task With_history_but_no_expenses_in_the_previous_period_it_compares_against_zero()
    {
        _movements.Income(_employeeId, new DateOnly(2026, 8, 1), 1000m);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.Alimentos);

        var breakdown = await RunSeptemberAsync();

        breakdown.HasPreviousPeriod.ShouldBeTrue();
        breakdown.Categories.ShouldHaveSingleItem().Comparison.ShouldBe(new ExpenseComparison(0m, 300m, null, true));
        breakdown.Consumption.Comparison.ShouldBe(new ExpenseComparison(0m, 300m, null, true));
        breakdown.Savings.Comparison.ShouldBe(new ExpenseComparison(0m, 0m, null, false));
    }

    [Fact]
    public async Task A_previous_period_with_partial_history_is_compared_with_what_it_has()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 7, 10), 100m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 10), 150m, ExpenseCategory.Alimentos);

        var breakdown = await RunAsync(new DateOnly(2026, 8, 1), EndOfSeptember);

        breakdown.HasPreviousPeriod.ShouldBeTrue();
        breakdown.Categories.ShouldHaveSingleItem().Comparison.ShouldBe(new ExpenseComparison(100m, 50m, 50.0m, false));
    }

    [Fact]
    public async Task Savings_have_their_own_variation()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 5), 100m, ExpenseCategory.AhorroInversion);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 5), 300m, ExpenseCategory.AhorroInversion);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 6), 200m, ExpenseCategory.Alimentos);

        var breakdown = await RunSeptemberAsync();

        breakdown.Savings.ShouldBe(new ExpenseTotal(300m, new ExpenseComparison(100m, 200m, 200.0m, false)));
        breakdown.Total.ShouldBe(new ExpenseTotal(500m, new ExpenseComparison(100m, 400m, 400.0m, false)));
        breakdown.Categories.ShouldNotContain(category => category.Category == ExpenseCategory.AhorroInversion);
    }

    [Fact]
    public async Task Only_the_requested_employee_currency_and_days_are_counted()
    {
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 10), 300m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 11), 50m, ExpenseCategory.Alimentos, Currency.USD);
        _movements.Expense(_employeeId, new DateOnly(2026, 10, 1), 700m, ExpenseCategory.Alimentos);
        _movements.Expense(Guid.CreateVersion7(), new DateOnly(2026, 9, 12), 9000m, ExpenseCategory.Alimentos);

        var breakdown = await RunSeptemberAsync();

        breakdown.Total.Amount.ShouldBe(300m);
    }

    [Fact]
    public async Task Without_expenses_the_breakdown_is_empty()
    {
        var breakdown = await RunSeptemberAsync();

        breakdown.Categories.ShouldBeEmpty();
        breakdown.Consumption.ShouldBe(new ExpenseTotal(0m, null));
        breakdown.Total.ShouldBe(new ExpenseTotal(0m, null));
    }

    [Theory]
    [InlineData("2026-09-30", "2026-09-01")]
    [InlineData("2023-01-01", "2026-01-31")]
    public void A_reversed_or_too_long_period_is_rejected(string from, string to)
    {
        var query = new GetExpenseBreakdownQuery(_employeeId, Currency.ARS, Parse(from), Parse(to));

        new GetExpenseBreakdownValidator().Validate(query).IsValid.ShouldBeFalse();
    }

    private static DateOnly Parse(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);
}
