namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionReceiptRepository(FinGrowDbContext dbContext) : ITransactionReceiptRepository
{
    public void Add(TransactionReceipt receipt) => dbContext.TransactionReceipts.Add(receipt);

    public Task<TransactionReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TransactionReceipts.FirstOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);
}
