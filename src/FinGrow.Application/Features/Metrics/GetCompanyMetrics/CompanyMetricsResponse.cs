namespace FinGrow.Application.Features.Metrics.GetCompanyMetrics;

using FinGrow.Domain.Entities;

public sealed record CompanyMetricsResponse(
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
    public static CompanyMetricsResponse FromSnapshot(CompanyMetricsSnapshot snapshot) =>
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

