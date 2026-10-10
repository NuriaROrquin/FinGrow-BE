namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface ITransactionReceiptRepository
{
    void Add(TransactionReceipt receipt);

    Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
