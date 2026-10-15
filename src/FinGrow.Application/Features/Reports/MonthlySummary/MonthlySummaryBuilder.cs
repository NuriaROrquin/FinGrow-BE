namespace FinGrow.Application.Features.Reports.MonthlySummary;

using Domain.Enums;
using Interfaces;

internal static class MonthlySummaryBuilder
{
    public static IReadOnlyList<MonthlySummaryMonth> Build(
        IReadOnlyCollection<MonthlyMovementTotal> totals,
        IReadOnlyList<DateOnly> months)
    {
        var totalsByMonth = totals.ToLookup(total => (total.Year, total.Month));

        return months
            .Select(month => BuildMonth(month, totalsByMonth[(month.Year, month.Month)].ToList()))
            .ToList();
    }

    public static IReadOnlyList<CategoryAmount> Combine(IEnumerable<IReadOnlyList<CategoryAmount>> categories) =>
        categories
            .SelectMany(amounts => amounts)
            .GroupBy(amount => amount.Category)
            .Select(group => new CategoryAmount(group.Key, group.Sum(amount => amount.Amount)))
            .OrderByDescending(amount => amount.Amount)
            .ThenBy(amount => amount.Category)
            .ToList();

    private static MonthlySummaryMonth BuildMonth(DateOnly month, IReadOnlyCollection<MonthlyMovementTotal> totals)
    {
        var income = totals.Where(total => total.Type == TransactionType.Income).Sum(total => total.Total);
        var expenses = totals.Where(total => total.Type == TransactionType.Expense).ToList();
        var spent = expenses.Sum(total => total.Total);
        var saved = expenses.Where(total => total.Category == ExpenseCategory.AhorroInversion).Sum(total => total.Total);

        var byCategory = expenses
            .Where(total => total.Category is not null)
            .GroupBy(total => total.Category!.Value)
            .Select(group => new CategoryAmount(group.Key, group.Sum(total => total.Total)))
            .Where(amount => amount.Amount > 0m)
            .OrderByDescending(amount => amount.Amount)
            .ThenBy(amount => amount.Category)
            .ToList();

        return new MonthlySummaryMonth(month.Year, month.Month, income, spent, income - spent + saved, byCategory);
    }
}
