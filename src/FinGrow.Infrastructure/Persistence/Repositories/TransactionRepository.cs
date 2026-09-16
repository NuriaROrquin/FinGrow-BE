namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionRepository(FinGrowDbContext dbContext) : ITransactionRepository
{
    public void Add(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .Where(transaction => transaction.EmployeeId == employeeId)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
}
