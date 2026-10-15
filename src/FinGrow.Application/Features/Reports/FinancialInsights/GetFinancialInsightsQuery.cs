namespace FinGrow.Application.Features.Reports.FinancialInsights;

using Common;
using Domain.Enums;
using DTOs;
using Interfaces;
using MediatR;
using MonthlySummary;

public sealed record GetFinancialInsightsQuery(Guid EmployeeId, Currency Currency, DateOnly From, DateOnly To)
    : IRequest<Result<FinancialInsightsResponse>>;

public sealed record FinancialInsightsResponse(
    Currency Currency,
    DateOnly From,
    DateOnly To,
    InsightsEligibility Eligibility,
    FinancialHighlights? Highlights,
    IReadOnlyList<AchievementResponse> Achievements,
    IReadOnlyList<FinancialRecommendation> Recommendations,
    bool RecommendationsAvailable)
{
    public static FinancialInsightsResponse NotEligible(GetFinancialInsightsQuery query, InsightsEligibility eligibility)
    {
        ArgumentNullException.ThrowIfNull(query);

        return new FinancialInsightsResponse(
            query.Currency,
            query.From,
            query.To,
            eligibility,
            null,
            Array.Empty<AchievementResponse>(),
            Array.Empty<FinancialRecommendation>(),
            false);
    }
}

public sealed record InsightsEligibility(
    bool IsEligible,
    int ActiveMonths,
    int RequiredActiveMonths,
    int CurrentMonthTransactions,
    int RequiredTransactions,
    bool CurrentMonthHasIncome);

public sealed record FinancialHighlights(
    int MonthCount,
    bool HasMovements,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal TotalSavings,
    decimal? SavingsRate,
    MonthAmount? HighestIncomeMonth,
    MonthAmount? HighestExpenseMonth,
    MonthAmount? BestSavingsMonth)
{
    public static FinancialHighlights From(IReadOnlyList<MonthlySummaryMonth> months)
    {
        ArgumentNullException.ThrowIfNull(months);

        var totalIncome = months.Sum(month => month.Income);
        var totalSavings = months.Sum(month => month.Savings);

        return new FinancialHighlights(
            months.Count,
            months.Any(month => month.HasMovements),
            totalIncome,
            months.Sum(month => month.Expenses),
            totalSavings,
            totalIncome > 0m ? decimal.Round(totalSavings / totalIncome * 100m, 1) : null,
            Highest(months, month => month.Income),
            Highest(months, month => month.Expenses),
            Highest(months.Where(month => month.HasMovements), month => month.Savings));
    }

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
