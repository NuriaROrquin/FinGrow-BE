namespace FinGrow.Application.DTOs.Transactions;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public record TransactionResponse(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    string Category,
    string Description,
    DateOnly OccurredOn,
    PaymentMethod PaymentMethod,
    TransactionSource Source,
    TransactionStatus Status,
    DateTimeOffset CreatedAt)
{
    public static TransactionResponse FromEntity(Transaction transaction) => new(
        transaction.Id,
        transaction.Type,
        transaction.Amount.Amount,
        transaction.Amount.Currency,
        transaction.ExpenseCategory?.ToString() ?? transaction.IncomeCategory?.ToString() ?? string.Empty,
        transaction.Description,
        transaction.OccurredOn,
        transaction.PaymentMethod,
        transaction.Source,
        transaction.Status,
        transaction.CreatedAt);
}
