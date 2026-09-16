namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface ITransactionRepository
{
    void Add(Transaction transaction);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
