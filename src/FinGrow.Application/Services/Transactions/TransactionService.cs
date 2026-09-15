namespace FinGrow.Application.Services.Transactions;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories.Transactions;
using FinGrow.Domain.Services.Transactions;
using FinGrow.Domain.ValueObjects;

public class TransactionService(ITransactionRepository transactionRepository) : ITransactionService
{
    public async Task<Transaction> CreateAsync(CreateTransactionInput input, CancellationToken cancellationToken = default)
    {
        var money = Money.From(input.Amount, input.Currency);
        var createdAt = DateTime.UtcNow;

        var transaction = input.Type == TransactionType.Expense
            ? Transaction.RegisterExpense(
                input.EmployeeId,
                money,
                input.ExpenseCategory!.Value,
                input.Description,
                input.OccurredOn,
                input.PaymentMethod,
                TransactionSource.Manual,
                TransactionStatus.Confirmed,
                createdAt)
            : Transaction.RegisterIncome(
                input.EmployeeId,
                money,
                input.IncomeCategory!.Value,
                input.Description,
                input.OccurredOn,
                input.PaymentMethod,
                TransactionSource.Manual,
                TransactionStatus.Confirmed,
                createdAt);

        await transactionRepository.AddAsync(transaction, cancellationToken);

        return transaction;
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        transactionRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        transactionRepository.ListByEmployeeAsync(employeeId, cancellationToken);
}
