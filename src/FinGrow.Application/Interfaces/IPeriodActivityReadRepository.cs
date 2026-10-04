namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.ValueObjects;

public interface IPeriodActivityReadRepository
{
    Task<IReadOnlyList<EmployeePeriodActivity>> ListByCompanyAsync(
        Guid companyId,
        MetricsPeriod period,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeePeriodActivity(
    Guid EmployeeId,
    Guid? DepartmentId,
    int ConfirmedTransactions,
    int GoalContributions,
    bool HasBudget,
    bool HasActiveGoal,
    int GoalsAchieved,
    bool HasIntegration)
{
    public bool IsParticipating => ConfirmedTransactions > 0 || GoalContributions > 0;
}
