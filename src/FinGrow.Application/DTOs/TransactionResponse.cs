namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public sealed record TransactionResponse(
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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    double? AiConfidence,
    ConfidenceLevel? AiConfidenceLevel,
    Guid? ReceiptId)
{
    public static TransactionResponse FromEntity(Transaction transaction, Guid? receiptId) => new(
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
        transaction.CreatedAt,
        transaction.UpdatedAt,
        transaction.AiConfidence,
        transaction.AiConfidenceLevel,
        receiptId);
}
