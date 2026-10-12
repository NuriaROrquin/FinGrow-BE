namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionReceiptRepository(FinGrowDbContext dbContext) : ITransactionReceiptRepository
{
    public void Add(TransactionReceipt receipt) => dbContext.TransactionReceipts.Add(receipt);

    public void Remove(TransactionReceipt receipt) => dbContext.TransactionReceipts.Remove(receipt);

    public Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TransactionReceipts.FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);

    public Task<TransactionReceipt?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        dbContext.TransactionReceipts.FirstOrDefaultAsync(receipt => receipt.TransactionId == transactionId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetIdsByTransactionAsync(
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default)
    {
        if (transactionIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        return await dbContext.TransactionReceipts
            .AsNoTracking()
            .Where(receipt => receipt.TransactionId != null && transactionIds.Contains(receipt.TransactionId.Value))
            .ToDictionaryAsync(receipt => receipt.TransactionId!.Value, receipt => receipt.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListStorageKeysByEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TransactionReceipts
            .AsNoTracking()
            .Where(receipt => receipt.EmployeeId == employeeId)
            .Select(receipt => receipt.StorageKey)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TransactionReceipt>> ListDetachedUploadedBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default) =>
        await dbContext.TransactionReceipts
            .Where(receipt => receipt.TransactionId == null && receipt.UploadedAt < cutoff)
            .OrderBy(receipt => receipt.UploadedAt)
            .ToListAsync(cancellationToken);
}
