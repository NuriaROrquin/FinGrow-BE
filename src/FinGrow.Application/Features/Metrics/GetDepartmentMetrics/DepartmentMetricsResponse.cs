namespace FinGrow.Application.Features.Metrics.GetDepartmentMetrics;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using System.Text.Json.Serialization;

public sealed record DepartmentMetricsResponse(
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
    [JsonIgnore]
    public FinancialHealthBand? HealthBand => FinancialHealthBand;

    public static DepartmentMetricsResponse FromSnapshot(
        DepartmentMetricsSnapshot snapshot,
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
