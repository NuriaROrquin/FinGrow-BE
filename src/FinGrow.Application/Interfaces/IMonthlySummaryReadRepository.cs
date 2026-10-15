namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Enums;

public interface IMonthlySummaryReadRepository
{
    Task<IReadOnlyList<MonthlyMovementTotal>> ListMonthlyTotalsAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>Fecha del primer movimiento confirmado del empleado en esa moneda, en todo su historial.</summary>
    Task<DateOnly?> FindFirstMovementDateAsync(
        Guid employeeId,
        Currency currency,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyActivity>> ListMonthlyActivityAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

public sealed record MonthlyMovementTotal(
    int Year,
    int Month,
    TransactionType Type,
    ExpenseCategory? Category,
    decimal Total);

public sealed record MonthlyActivity(int Year, int Month, int ConfirmedTransactions, bool HasIncome);
