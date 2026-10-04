namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface IBudgetRepository
{
    void Add(Budget budget);

    void Remove(Budget budget);

    Task<bool> ExistsForPeriodAsync(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        CancellationToken cancellationToken = default);

    Task<Budget?> FindForPeriodAsync(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        CancellationToken cancellationToken = default);
}
