namespace FinGrow.Api.Contracts;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

public sealed class TransactionQueryParameters
{
    [FromQuery]
    public int PageNumber { get; init; } = 1;

    [FromQuery]
    public int PageSize { get; init; } = 20;

    [FromQuery]
    public string? Search { get; init; }

    [FromQuery]
    public TransactionType? Type { get; init; }

    [FromQuery]
    public ExpenseCategory? ExpenseCategory { get; init; }

    [FromQuery]
    public IncomeCategory? IncomeCategory { get; init; }

    [FromQuery]
    public TransactionStatus[]? Status { get; init; }

    [FromQuery]
    public PaymentMethod? PaymentMethod { get; init; }

    [FromQuery(Name = "dateFrom")]
    public DateOnly? DateFrom { get; init; }

    [FromQuery(Name = "dateTo")]
    public DateOnly? DateTo { get; init; }

    [FromQuery]
    public TransactionSortField SortBy { get; init; } = TransactionSortField.OccurredOn;

    [FromQuery]
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    public TransactionFilters ToFilters() =>
        new(
            PageNumber,
            PageSize,
            Search,
            Type,
            ExpenseCategory,
            IncomeCategory,
            Status,
            PaymentMethod,
            DateFrom,
            DateTo,
            SortBy,
            SortDirection);
}
