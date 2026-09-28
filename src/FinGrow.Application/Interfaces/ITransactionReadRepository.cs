namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface ITransactionReadRepository
{
    Task<TransactionSummary> GetSummaryAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<TransactionPage> GetPageAsync(
        Guid employeeId,
        int pageNumber,
        int pageSize,
        string? search,
        TransactionType? type,
        TransactionStatus? status,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);
}

/// <summary>Totales sobre la totalidad de movimientos confirmados del empleado, sin paginar.</summary>
public sealed record TransactionSummary(
    int TotalTransactions,
    int TotalExpenseTransactions,
    int TotalIncomeTransactions,
    decimal TotalIncomeArs,
    decimal TotalIncomeUsd,
    decimal TotalExpenseArs,
    decimal TotalExpenseUsd);

public sealed record TransactionPage(
    IReadOnlyList<Transaction> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}