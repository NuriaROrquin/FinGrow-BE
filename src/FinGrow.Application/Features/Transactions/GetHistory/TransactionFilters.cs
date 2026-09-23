namespace FinGrow.Application.Features.Transactions.GetHistory;

using FinGrow.Domain.Enums;

public sealed record TransactionFilters(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    TransactionType? Type = null,
    ExpenseCategory? ExpenseCategory = null,
    IncomeCategory? IncomeCategory = null,
    TransactionStatus? Status = null,
    PaymentMethod? PaymentMethod = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);
