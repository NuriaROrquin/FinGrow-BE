namespace FinGrow.Application.UnitTests.Features.Reports.MonthlySummary;

using FinGrow.Application.Features.Reports.MonthlySummary;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Application.Validations.Reports;
using FinGrow.Domain.Enums;

public class GetMonthlySummaryHandlerTests
{
    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeMonthlySummaryReadRepository _movements = new();

    private async Task<MonthlySummaryResponse> RunAsync(DateOnly from, DateOnly to, Currency currency = Currency.ARS) =>
        (await new GetMonthlySummaryHandler(_movements)
            .Handle(new GetMonthlySummaryQuery(_employeeId, currency, from, to), CancellationToken.None)).Value;

    [Fact]
    public async Task Every_month_of_the_period_appears_even_without_movements()
    {
        _movements.Income(_employeeId, new DateOnly(2026, 8, 5), 1000m);

        var summary = await RunAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 30));

        summary.Months.Select(month => (month.Year, month.Month)).ShouldBe(new[] { (2026, 7), (2026, 8), (2026, 9) });
        summary.Months[0].Income.ShouldBe(0m);
        summary.Months[0].ExpensesByCategory.ShouldBeEmpty();
    }

    [Fact]
    public async Task Savings_count_what_went_to_ahorro_inversion_as_saved()
    {
        var month = new DateOnly(2026, 9, 10);
        _movements.Income(_employeeId, month, 1000m);
        _movements.Expense(_employeeId, month, 300m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, month, 100m, ExpenseCategory.Alimentos);
        _movements.Expense(_employeeId, month, 200m, ExpenseCategory.AhorroInversion);

        var summary = await RunAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var september = summary.Months.ShouldHaveSingleItem();
        september.Income.ShouldBe(1000m);
        september.Expenses.ShouldBe(600m);
        september.Savings.ShouldBe(600m);
        september.ExpensesByCategory.ShouldBe(new[]
        {
            new CategoryAmount(ExpenseCategory.Alimentos, 400m),
            new CategoryAmount(ExpenseCategory.AhorroInversion, 200m),
        });
    }

    [Fact]
    public async Task Totals_add_up_the_months_and_the_categories_of_the_period()
    {
        _movements.Income(_employeeId, new DateOnly(2026, 8, 1), 1000m);
        _movements.Income(_employeeId, new DateOnly(2026, 9, 1), 1500m);
        _movements.Expense(_employeeId, new DateOnly(2026, 8, 2), 400m, ExpenseCategory.Transporte);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 2), 300m, ExpenseCategory.Transporte);
        _movements.Expense(_employeeId, new DateOnly(2026, 9, 3), 500m, ExpenseCategory.Vivienda);

        var summary = await RunAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 30));

        summary.TotalIncome.ShouldBe(2500m);
        summary.TotalExpenses.ShouldBe(1200m);
        summary.TotalSavings.ShouldBe(1300m);
        summary.ExpensesByCategory.ShouldBe(new[]
        {
            new CategoryAmount(ExpenseCategory.Transporte, 700m),
            new CategoryAmount(ExpenseCategory.Vivienda, 500m),
        });
    }

    [Fact]
    public async Task Only_the_requested_currency_and_days_are_counted()
    {
        _movements.Income(_employeeId, new DateOnly(2026, 9, 10), 1000m);
        _movements.Income(_employeeId, new DateOnly(2026, 9, 11), 50m, Currency.USD);
        _movements.Income(_employeeId, new DateOnly(2026, 9, 2), 700m);
        _movements.Income(Guid.CreateVersion7(), new DateOnly(2026, 9, 12), 9000m);

        var summary = await RunAsync(new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 30));

        summary.TotalIncome.ShouldBe(1000m);
    }

    [Theory]
    [InlineData("2026-09-30", "2026-09-01")]
    [InlineData("2023-01-01", "2026-01-31")]
    public void A_reversed_or_too_long_period_is_rejected(string from, string to)
    {
        var query = new GetMonthlySummaryQuery(_employeeId, Currency.ARS, DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture));

        new GetMonthlySummaryValidator().Validate(query).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Thirty_six_months_is_the_longest_period_accepted()
    {
        var query = new GetMonthlySummaryQuery(_employeeId, Currency.ARS, new DateOnly(2023, 11, 1), new DateOnly(2026, 10, 15));

        new GetMonthlySummaryValidator().Validate(query).IsValid.ShouldBeTrue();
    }
}
