namespace FinGrow.Application.Features.Transactions.ConfirmTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record ConfirmTransactionCommand(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    ExpenseCategory? ExpenseCategory,
    IncomeCategory? IncomeCategory,
    string Description,
    DateOnly OccurredOn,
    PaymentMethod PaymentMethod) : IRequest<Result<TransactionResponse>>;
