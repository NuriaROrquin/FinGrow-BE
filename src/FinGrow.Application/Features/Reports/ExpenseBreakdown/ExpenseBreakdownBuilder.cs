namespace FinGrow.Application.Features.Reports.ExpenseBreakdown;

using Domain.Enums;
using Interfaces;
using MonthlySummary;

internal static class ExpenseBreakdownBuilder
{
    public static PeriodExpenses Summarize(IReadOnlyCollection<MonthlyMovementTotal> totals, DateOnly from, DateOnly to)
    {
        var months = MonthlySummaryBuilder.Build(totals, ReportPeriod.Months(from, to));
        var categories = MonthlySummaryBuilder.Combine(months.Select(month => month.ExpensesByCategory));

        return new PeriodExpenses(categories, months.Sum(month => month.Expenses));
    }

    public static IReadOnlyList<CategoryExpense> Categories(PeriodExpenses current, PeriodExpenses? previous)
    {
        var consumption = current.Consumption;

        return current.Categories
            .Select(amount => amount.Category)
            .Union(previous?.Categories.Select(amount => amount.Category) ?? Enumerable.Empty<ExpenseCategory>())
            .Where(category => category != ExpenseCategory.AhorroInversion)
            .Select(category =>
            {
                var amount = current.AmountOf(category);

                return new CategoryExpense(
                    category,
                    amount,
                    Percent(amount, consumption),
                    previous is null ? null : Compare(amount, previous.AmountOf(category)));
            })
            .OrderByDescending(category => category.Amount)
            .ThenByDescending(category => category.Comparison?.PreviousAmount ?? 0m)
            .ThenBy(category => category.Category)
            .ToList();
    }

    public static ExpenseTotal Total(decimal current, decimal? previous) =>
        new(current, previous is null ? null : Compare(current, previous.Value));

    private static ExpenseComparison Compare(decimal current, decimal previous) =>
        new(
            previous,
            current - previous,
            previous == 0m ? null : Percent(current - previous, previous),
            previous == 0m && current > 0m);

    private static decimal Percent(decimal part, decimal whole) =>
        whole == 0m ? 0m : decimal.Round(part / whole * 100m, 1, MidpointRounding.AwayFromZero);
}

internal sealed record PeriodExpenses(IReadOnlyList<CategoryAmount> Categories, decimal Total)
{
    public decimal Savings => AmountOf(ExpenseCategory.AhorroInversion);

    public decimal Consumption => Total - Savings;

    public decimal AmountOf(ExpenseCategory category) =>
        Categories.Where(amount => amount.Category == category).Sum(amount => amount.Amount);
}
