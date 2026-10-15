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
