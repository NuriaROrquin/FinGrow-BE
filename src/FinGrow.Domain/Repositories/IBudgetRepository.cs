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

    /// <summary>
    /// Los presupuestos mensuales del empleado cuyo período empieza entre las dos fechas (inclusive),
    /// del más viejo al más nuevo.
    /// </summary>
    Task<IReadOnlyList<Budget>> ListMonthlyAsync(
        Guid employeeId,
        DateOnly fromPeriodStart,
        DateOnly toPeriodStart,
        CancellationToken cancellationToken = default);
}
