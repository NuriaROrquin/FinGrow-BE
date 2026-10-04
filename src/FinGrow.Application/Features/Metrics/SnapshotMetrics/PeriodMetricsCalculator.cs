namespace FinGrow.Application.Features.Metrics.SnapshotMetrics;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.ValueObjects;

internal static class PeriodMetricsCalculator
{
    public static PeriodMetrics Aggregate(IEnumerable<EmployeePeriodActivity> activity)
    {
        var employees = activity.ToList();

        return PeriodMetrics.From(
            activeEmployees: employees.Count,
            participatingEmployees: employees.Count(employee => employee.IsParticipating),
            confirmedTransactions: employees.Sum(employee => employee.ConfirmedTransactions),
            employeesWithBudget: employees.Count(employee => employee.HasBudget),
            employeesWithActiveGoal: employees.Count(employee => employee.HasActiveGoal),
            goalsAchieved: employees.Sum(employee => employee.GoalsAchieved),
            employeesWithIntegration: employees.Count(employee => employee.HasIntegration));
    }
}
