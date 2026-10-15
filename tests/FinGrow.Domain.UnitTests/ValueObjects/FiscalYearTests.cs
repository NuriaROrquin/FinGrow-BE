namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.ValueObjects;

public sealed class FiscalYearTests
{
    [Theory]
    [InlineData(2026, 3, 4, 2025)]
    [InlineData(2026, 4, 4, 2026)]
    [InlineData(2027, 1, 4, 2026)]
    public void A_period_is_assigned_to_the_fiscal_year_containing_it(
        int year,
        int month,
        int startMonth,
        int expectedStartYear)
    {
        var fiscalYear = FiscalYear.From(new DateOnly(year, month, 1), startMonth);

        fiscalYear.StartYear.ShouldBe(expectedStartYear);
        fiscalYear.StartMonth.ShouldBe(startMonth);
    }
}
