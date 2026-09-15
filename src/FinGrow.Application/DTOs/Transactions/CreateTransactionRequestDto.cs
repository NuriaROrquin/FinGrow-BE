namespace FinGrow.Application.DTOs.Transactions;

using FinGrow.Domain.Enums;

public record CreateTransactionRequestDto(
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    ExpenseCategory? ExpenseCategory,
    IncomeCategory? IncomeCategory,
    string Description,
    DateOnly OccurredOn,
    PaymentMethod PaymentMethod);
