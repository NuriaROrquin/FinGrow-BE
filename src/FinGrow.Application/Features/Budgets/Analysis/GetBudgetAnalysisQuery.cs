namespace FinGrow.Application.Features.Budgets.Analysis;

using Common;
using Domain.Enums;
using Interfaces;
using MediatR;

public sealed record GetBudgetAnalysisQuery(Guid EmployeeId, Currency Currency)
    : IRequest<Result<BudgetAnalysisResponse>>;

public sealed record BudgetAnalysisResponse(
    Currency Currency,
    BudgetAnalysisEligibility Eligibility,
    BudgetAnalysisSummary? Summary,
    IReadOnlyList<FinancialRecommendation> Recommendations,
    bool RecommendationsAvailable)
{
    public static BudgetAnalysisResponse NotEligible(Currency currency, BudgetAnalysisEligibility eligibility) =>
        new(currency, eligibility, null, Array.Empty<FinancialRecommendation>(), false);
}

/// <summary>
/// Cuántos meses con presupuesto en la moneda hay entre los analizados y el mes en curso, y cuántos hacen
/// falta para el análisis. El front lo usa para explicar qué falta (HU-25, criterio 3).
/// </summary>
public sealed record BudgetAnalysisEligibility(
    bool IsEligible,
    int BudgetMonths,
    int RequiredBudgetMonths,
    int AnalyzedMonths,
    bool HasCurrentMonthBudget);

/// <summary>
/// Los datos que salen de reglas, sin IA: se muestran aunque FinGrow-AI no responda (HU-25, criterio 2).
/// </summary>
public sealed record BudgetAnalysisSummary(
    DateOnly FirstMonth,
    DateOnly LastMonth,
    int ClosedBudgets,
    BudgetMonthCompliance LastClosedMonth,
    ExpenseCategory? MostExceededCategory,
    IReadOnlyList<BudgetCategoryAnalysis> Categories);

public sealed record BudgetMonthCompliance(
    int Year,
    int Month,
    int CategoriesWithLimit,
    int CategoriesWithinLimit,
    decimal TotalLimit,
    decimal TotalSpent);

public sealed record BudgetCategoryAnalysis(
    ExpenseCategory Category,
    int MonthsWithLimit,
    int MonthsExceeded,
    decimal AverageLimit,
    decimal AverageSpent,
    decimal AverageUsedPercentage,
    DateOnly? LastExceededMonth);
