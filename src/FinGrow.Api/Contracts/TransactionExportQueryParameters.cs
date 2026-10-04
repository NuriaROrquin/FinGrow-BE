namespace FinGrow.Api.Contracts;

using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

public sealed class TransactionExportQueryParameters
{
    [FromQuery]
    public string? Search { get; init; }

    [FromQuery(Name = "transactionType")]
    public TransactionType? Type { get; init; }

    [FromQuery]
    public ExpenseCategory? ExpenseCategory { get; init; }

    [FromQuery]
    public IncomeCategory? IncomeCategory { get; init; }

    [FromQuery(Name = "transactionStatus")]
    public TransactionStatus[]? Status { get; init; }

    [FromQuery]
    public PaymentMethod? PaymentMethod { get; init; }

    [FromQuery(Name = "dateFrom")]
    public DateOnly? DateFrom { get; init; }

    [FromQuery(Name = "dateTo")]
    public DateOnly? DateTo { get; init; }

    public TransactionFilters ToFilters() =>
        new(Search: Search,
            Type: Type,
            ExpenseCategory: ExpenseCategory,
            IncomeCategory: IncomeCategory,
            Status: Status,
            PaymentMethod: PaymentMethod,
            DateFrom: DateFrom,
            DateTo: DateTo);
}