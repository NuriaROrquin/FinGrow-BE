namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface ITransactionReceiptRepository
{
    void Add(TransactionReceipt receipt);

    void Remove(TransactionReceipt receipt);

    Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TransactionReceipt?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, Guid>> GetIdsByTransactionAsync(
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListStorageKeysByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionReceipt>> ListDetachedUploadedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default);
}
