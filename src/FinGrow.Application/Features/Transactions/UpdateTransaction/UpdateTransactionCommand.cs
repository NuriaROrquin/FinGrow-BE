namespace FinGrow.Application.Features.Transactions.UpdateTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record UpdateTransactionCommand(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    ExpenseCategory? ExpenseCategory,
    IncomeCategory? IncomeCategory,
    string Description,
    DateOnly OccurredOn,
    PaymentMethod PaymentMethod,
    TransactionStatus Status) : IRequest<Result<TransactionResponse>>;