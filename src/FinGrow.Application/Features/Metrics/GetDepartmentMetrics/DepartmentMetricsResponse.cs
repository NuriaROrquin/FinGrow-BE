namespace FinGrow.Application.Features.Metrics.GetDepartmentMetrics;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public sealed record DepartmentMetricsResponse(
    DateOnly Period,
    int FiscalYear,
    int ActiveEmployees,
    decimal? ParticipationRate,
    int ConfirmedTransactions,
    int EmployeesWithBudget,
    int EmployeesWithActiveGoal,
    int GoalsAchieved,
    int EmployeesWithIntegration,
    decimal? AverageFinancialHealthScore,
    FinancialHealthBand? HealthBand)
{
    public static DepartmentMetricsResponse FromSnapshot(
        DepartmentMetricsSnapshot snapshot,
        int fiscalYearStartMonth,
        decimal? averageFinancialHealthScore) =>
        new(
            snapshot.PeriodStart,
            FinGrow.Domain.ValueObjects.FiscalYear.From(snapshot.PeriodStart, fiscalYearStartMonth).StartYear,
            snapshot.Metrics.ActiveEmployees,
            snapshot.Metrics.ParticipationRate,
            snapshot.Metrics.ConfirmedTransactions,
            snapshot.Metrics.EmployeesWithBudget,
            snapshot.Metrics.EmployeesWithActiveGoal,
            snapshot.Metrics.GoalsAchieved,
            snapshot.Metrics.EmployeesWithIntegration,
            averageFinancialHealthScore,
            averageFinancialHealthScore is { } score
                ? FinancialHealthScale.BandOf(score)
                : null);
}
