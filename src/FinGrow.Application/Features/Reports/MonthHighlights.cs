namespace FinGrow.Application.Features.Reports;

using MonthlySummary;

/// <summary>
/// El mes con mayor ingreso, el de mayor gasto y el mejor mes de ahorro de una serie (HU-41).
/// Lo usan el resumen mensual y los hitos de HU-44, así que los dos dicen lo mismo sobre los mismos meses.
/// </summary>
public sealed record MonthHighlights(
    MonthAmount? HighestIncomeMonth,
    MonthAmount? HighestExpenseMonth,
    MonthAmount? BestSavingsMonth)
{
    public static MonthHighlights From(IReadOnlyList<MonthlySummaryMonth> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        return new MonthHighlights(
            Highest(months, month => month.Income),
            Highest(months, month => month.Expenses),
            Highest(months.Where(month => month.HasMovements), month => month.Savings));
    }

    // Solo cuenta un monto positivo: un mes sin ingresos no es "el de mayor ingreso" ni un ahorro negativo
    // es "el mejor mes de ahorro". Ante un empate gana el mes más reciente.
    private static MonthAmount? Highest(IEnumerable<MonthlySummaryMonth> months, Func<MonthlySummaryMonth, decimal> amount) =>
        months
            .Where(month => amount(month) > 0m)
            .OrderByDescending(amount)
            .ThenByDescending(month => month.Year)
            .ThenByDescending(month => month.Month)
            .Select(month => new MonthAmount(month.Year, month.Month, amount(month)))
            .FirstOrDefault();
}

public sealed record MonthAmount(int Year, int Month, decimal Amount);
