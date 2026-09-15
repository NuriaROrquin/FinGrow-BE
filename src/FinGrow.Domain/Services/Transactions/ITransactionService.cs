namespace FinGrow.Domain.Services.Transactions;

using FinGrow.Domain.Entities;

public interface ITransactionService
{
    Task<Transaction> CreateAsync(CreateTransactionInput input, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
