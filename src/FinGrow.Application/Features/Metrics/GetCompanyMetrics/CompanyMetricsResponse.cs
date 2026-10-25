namespace FinGrow.Application.Features.Metrics.GetCompanyMetrics;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public sealed record CompanyMetricsResponse(
    DateOnly Period,
    int FiscalYear,
    int ActiveEmployees,
    int? ParticipatingEmployees,
    decimal? ParticipationRate,
    int? ConfirmedTransactions,
    int? EmployeesWithBudget,
    int? EmployeesWithActiveGoal,
    int? GoalsAchieved,
    int? EmployeesWithIntegration,
    decimal? AverageFinancialHealthScore,
    FinancialHealthBand? FinancialHealthBand,
    bool IsMetricsHidden,
    int MinimumGroupSize,
    string? HiddenReason)
{
    public static CompanyMetricsResponse FromSnapshot(
        CompanyMetricsSnapshot snapshot,
        int fiscalYearStartMonth,
        decimal? averageFinancialHealthScore,
        bool isMetricsHidden,
        int minimumGroupSize,
        string? hiddenReason) =>
        new(
            snapshot.PeriodStart,
            FinGrow.Domain.ValueObjects.FiscalYear.From(snapshot.PeriodStart, fiscalYearStartMonth).StartYear,
            snapshot.Metrics.ActiveEmployees,
            isMetricsHidden ? null : snapshot.Metrics.ParticipatingEmployees,
            isMetricsHidden ? null : snapshot.Metrics.ParticipationRate,
            isMetricsHidden ? null : snapshot.Metrics.ConfirmedTransactions,
            isMetricsHidden ? null : snapshot.Metrics.EmployeesWithBudget,
            isMetricsHidden ? null : snapshot.Metrics.EmployeesWithActiveGoal,
            isMetricsHidden ? null : snapshot.Metrics.GoalsAchieved,
            isMetricsHidden ? null : snapshot.Metrics.EmployeesWithIntegration,
            isMetricsHidden ? null : averageFinancialHealthScore,
            !isMetricsHidden && averageFinancialHealthScore is { } score
                ? FinancialHealthScale.BandOf(score)
                : null,
            isMetricsHidden,
            minimumGroupSize,
            hiddenReason);
}
