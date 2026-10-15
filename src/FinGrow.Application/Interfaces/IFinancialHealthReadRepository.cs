namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public interface IFinancialHealthReadRepository
{
    Task<IReadOnlyList<EmployeeCurrencyTotals>> ListMovementTotalsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        MetricsPeriod period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeBudgetLimit>> ListBudgetLimitsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        MetricsPeriod period,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeeCurrencyTotals(
    Guid EmployeeId,
    Currency Currency,
    DateOnly FirstMovementOn,
    decimal MonthIncome,
    decimal MonthSpending,
    decimal IncomeToDate,
    decimal SpendingToDate,
    decimal RecentSpending);

public sealed record EmployeeBudgetLimit(Guid EmployeeId, Money Limit, Money Spent);
