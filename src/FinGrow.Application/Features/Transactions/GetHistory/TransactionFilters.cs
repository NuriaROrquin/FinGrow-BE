namespace FinGrow.Application.Features.Transactions.GetHistory;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;

public sealed record TransactionFilters(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    TransactionType? Type = null,
    ExpenseCategory? ExpenseCategory = null,
    IncomeCategory? IncomeCategory = null,
    IReadOnlyCollection<TransactionStatus>? Status = null,
    PaymentMethod? PaymentMethod = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    TransactionSortField SortBy = TransactionSortField.OccurredOn,
    SortDirection SortDirection = SortDirection.Descending)
{
    public IReadOnlyCollection<TransactionStatus> EffectiveStatuses =>
        Status is { Count: > 0 }
            ? Status
            : new[] { TransactionStatus.Confirmed };
}
