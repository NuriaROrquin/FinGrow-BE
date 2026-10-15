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

    // HU-41: la serie no se rellena con ceros antes de que el empleado empezara a registrar movimientos en la
    // moneda (mirando todo su historial, no solo el período) ni con el mes en curso (o uno futuro) todavía vacío.
    // Un mes vacío después del primer movimiento sí queda: es un cero real y cuenta para el promedio.
    public static IReadOnlyList<MonthlySummaryMonth> TrimToHistory(
        IReadOnlyList<MonthlySummaryMonth> months,
        DateOnly? firstMovement,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(months);

        if (firstMovement is null)
        {
            return Array.Empty<MonthlySummaryMonth>();
        }

        var firstMonth = ReportPeriod.StartOfMonth(firstMovement.Value);
        var currentMonth = ReportPeriod.StartOfMonth(today);
        var trimmed = months.Where(month => StartOf(month) >= firstMonth).ToList();

        while (trimmed.Count > 0 && !trimmed[^1].HasMovements && StartOf(trimmed[^1]) >= currentMonth)
        {
            trimmed.RemoveAt(trimmed.Count - 1);
        }

        return trimmed;
    }

    public static IReadOnlyList<CategoryAmount> Combine(IEnumerable<IReadOnlyList<CategoryAmount>> categories) =>
        categories
            .SelectMany(amounts => amounts)
            .GroupBy(amount => amount.Category)
            .Select(group => new CategoryAmount(group.Key, group.Sum(amount => amount.Amount)))
            .OrderByDescending(amount => amount.Amount)
            .ThenBy(amount => amount.Category)
            .ToList();

    private static DateOnly StartOf(MonthlySummaryMonth month) => new(month.Year, month.Month, 1);

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
