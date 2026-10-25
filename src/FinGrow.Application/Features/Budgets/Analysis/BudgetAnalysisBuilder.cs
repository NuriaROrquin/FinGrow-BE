namespace FinGrow.Application.Features.Budgets.Analysis;

using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Interfaces;
using Reports.MonthlySummary;

/// <summary>Cómo le fue a un presupuesto de un mes: cada tope contra lo gastado y el gasto sin tope.</summary>
internal sealed record BudgetMonthUsage(
    DateOnly PeriodStart,
    Currency Currency,
    IReadOnlyList<RecommendationBudgetLimit> Limits,
    IReadOnlyDictionary<ExpenseCategory, decimal> UnbudgetedExpenses);

internal static class BudgetAnalysisBuilder
{
    // Lo gastado sale del resumen mensual (el mismo que usan los reportes), que cuenta los gastos
    // confirmados en la moneda del mes igual que el presupuesto de HU-23.
    public static IReadOnlyList<BudgetMonthUsage> Usage(
        IReadOnlyList<Budget> budgets,
        IReadOnlyList<MonthlySummaryMonth> months)
    {
        var monthsByStart = months.ToDictionary(month => new DateOnly(month.Year, month.Month, 1));

        return budgets
            .Select(budget => ToUsage(budget, monthsByStart.GetValueOrDefault(budget.PeriodStart)))
            .ToList();
    }

    public static BudgetAnalysisSummary Summarize(IReadOnlyList<BudgetMonthUsage> closed)
    {
        var categories = closed
            .SelectMany(month => month.Limits.Select(limit => (month.PeriodStart, month.Currency, Limit: limit)))
            .GroupBy(entry => entry.Limit.Category)
            .Select(group => new BudgetCategoryAnalysis(
                group.Key,
                group.Count(),
                group.Count(entry => IsExceeded(entry.Limit, entry.Currency)),
                decimal.Round(group.Average(entry => entry.Limit.Limit), 2, MidpointRounding.ToEven),
                decimal.Round(group.Average(entry => entry.Limit.Spent), 2, MidpointRounding.ToEven),
                decimal.Round(group.Average(entry => UsedPercentage(entry.Limit)), 1, MidpointRounding.ToEven),
                group
                    .Where(entry => IsExceeded(entry.Limit, entry.Currency))
                    .Select(entry => (DateOnly?)entry.PeriodStart)
                    .Max()))
            .OrderByDescending(category => category.MonthsExceeded)
            .ThenByDescending(category => category.AverageUsedPercentage)
            .ThenBy(category => category.Category)
            .ToList();

        var last = closed[^1];

        return new BudgetAnalysisSummary(
            closed[0].PeriodStart,
            last.PeriodStart,
            closed.Count,
            Compliance(last),
            categories
                .Where(category => IsConsumption(category.Category) && category.MonthsExceeded > 0)
                .Select(category => (ExpenseCategory?)category.Category)
                .FirstOrDefault(),
            categories);
    }

    private static BudgetMonthUsage ToUsage(Budget budget, MonthlySummaryMonth? month)
    {
        var spent = (month?.ExpensesByCategory ?? Array.Empty<CategoryAmount>())
            .ToDictionary(amount => amount.Category, amount => amount.Amount);

        return new BudgetMonthUsage(
            budget.PeriodStart,
            budget.Currency,
            budget.Limits
                .OrderBy(limit => limit.Category)
                .Select(limit => new RecommendationBudgetLimit(
                    limit.Category,
                    limit.Limit.Amount,
                    spent.GetValueOrDefault(limit.Category)))
                .ToList(),
            spent
                .Where(amount => budget.LimitFor(amount.Key) is null)
                .ToDictionary(amount => amount.Key, amount => amount.Value));
    }

    // Los conteos de cumplimiento dejan afuera ahorro_inversion: pasarse de ese tope es ahorrar de más,
    // no un desvío (el mismo criterio que el puntaje de salud y el desglose de gastos). Los totales sí lo
    // incluyen, para que coincidan con el "Presupuesto Total" de la pantalla.
    private static BudgetMonthCompliance Compliance(BudgetMonthUsage month)
    {
        var consumption = month.Limits.Where(limit => IsConsumption(limit.Category)).ToList();

        return new BudgetMonthCompliance(
            month.PeriodStart.Year,
            month.PeriodStart.Month,
            consumption.Count,
            consumption.Count(limit => !IsExceeded(limit, month.Currency)),
            month.Limits.Sum(limit => limit.Limit),
            month.Limits.Sum(limit => limit.Spent));
    }

    private static bool IsConsumption(ExpenseCategory category) => category != ExpenseCategory.AhorroInversion;

    private static bool IsExceeded(RecommendationBudgetLimit limit, Currency currency) =>
        Budget.HealthOf(Money.From(limit.Limit, currency), Money.From(limit.Spent, currency)) == BudgetHealth.Exceeded;

    private static decimal UsedPercentage(RecommendationBudgetLimit limit) => limit.Spent * 100m / limit.Limit;
}
