namespace FinGrow.Domain.Services.Transactions;

using FinGrow.Domain.Enums;

public record CreateTransactionInput(
    Guid EmployeeId,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    ExpenseCategory? ExpenseCategory,
    IncomeCategory? IncomeCategory,
    string Description,
    DateOnly OccurredOn,
    PaymentMethod PaymentMethod);
