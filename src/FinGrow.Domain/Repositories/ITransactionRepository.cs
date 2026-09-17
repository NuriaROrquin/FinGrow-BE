namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface ITransactionRepository
{
    void Add(Transaction transaction);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(
        Guid employeeId,
        TransactionSource source,
        IReadOnlyCollection<string> externalReferences,
        CancellationToken cancellationToken = default);
}
