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
    // Los logros del período no dependen del historial: sin la habilitación solo faltan hitos y recomendaciones.
    public static FinancialInsightsResponse NotEligible(
        GetFinancialInsightsQuery query,
        InsightsEligibility eligibility,
        IReadOnlyList<AchievementResponse> achievements)
    {
        ArgumentNullException.ThrowIfNull(query);

        return new FinancialInsightsResponse(
            query.Currency,
            query.From,
            query.To,
            eligibility,
            null,
            achievements,
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
        var monthHighlights = MonthHighlights.From(months);

        return new FinancialHighlights(
            months.Count,
            months.Any(month => month.HasMovements),
            totalIncome,
            months.Sum(month => month.Expenses),
            totalSavings,
            totalIncome > 0m ? decimal.Round(totalSavings / totalIncome * 100m, 1) : null,
            monthHighlights.HighestIncomeMonth,
            monthHighlights.HighestExpenseMonth,
            monthHighlights.BestSavingsMonth);
    }
}
