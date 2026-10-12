namespace FinGrow.Application.Features.Metrics.GetDepartmentMetrics;

using FinGrow.Domain.Entities;

public sealed record DepartmentMetricsResponse(
    DateOnly Period,
    int ActiveEmployees,
    decimal? ParticipationRate,
    int ConfirmedTransactions,
    int EmployeesWithBudget,
    int EmployeesWithActiveGoal,
    int GoalsAchieved,
    int EmployeesWithIntegration,
    decimal? AverageFinancialHealthScore)
{
    public static DepartmentMetricsResponse FromSnapshot(DepartmentMetricsSnapshot snapshot) =>
        new(
            snapshot.PeriodStart,
            snapshot.Metrics.ActiveEmployees,
            snapshot.Metrics.ParticipationRate,
            snapshot.Metrics.ConfirmedTransactions,
            snapshot.Metrics.EmployeesWithBudget,
            snapshot.Metrics.EmployeesWithActiveGoal,
            snapshot.Metrics.GoalsAchieved,
            snapshot.Metrics.EmployeesWithIntegration,
            snapshot.Metrics.AverageFinancialHealthScore);
}

