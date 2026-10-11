namespace FinGrow.Application.Features.Metrics.SnapshotMetrics;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.ValueObjects;

internal static class PeriodMetricsCalculator
{
    public static PeriodMetrics Aggregate(IEnumerable<EmployeePeriodActivity> activity, IReadOnlyDictionary<Guid, int> scores)
    {
        var employees = activity.ToList();
        var employeeScores = employees
            .Select(employee => scores.TryGetValue(employee.EmployeeId, out var score) ? score : (int?)null)
            .OfType<int>()
            .ToList();

        return PeriodMetrics.From(
            activeEmployees: employees.Count,
            participatingEmployees: employees.Count(employee => employee.IsParticipating),
            confirmedTransactions: employees.Sum(employee => employee.ConfirmedTransactions),
            employeesWithBudget: employees.Count(employee => employee.HasBudget),
            employeesWithActiveGoal: employees.Count(employee => employee.HasActiveGoal),
            goalsAchieved: employees.Sum(employee => employee.GoalsAchieved),
            employeesWithIntegration: employees.Count(employee => employee.HasIntegration),
            employeesWithScore: employeeScores.Count,
            financialHealthScoreSum: employeeScores.Sum());
    }
}
