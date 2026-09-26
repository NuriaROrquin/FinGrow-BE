namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionRepository(FinGrowDbContext dbContext) : ITransactionRepository
{
    public void Add(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Transactions.FirstOrDefaultAsync(
            transaction => transaction.Id == id && transaction.Status != TransactionStatus.Eliminated,
            cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status != TransactionStatus.Eliminated)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> ListExistingExternalReferencesAsync(
        Guid employeeId,
        TransactionSource source,
        IReadOnlyCollection<string> externalReferences,
        CancellationToken cancellationToken = default)
    {
        if (externalReferences.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var found = await dbContext.Transactions
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Source == source
                && transaction.ExternalReference != null
                && externalReferences.Contains(transaction.ExternalReference))
            .Select(transaction => transaction.ExternalReference!)
            .ToListAsync(cancellationToken);

        return found.ToHashSet(StringComparer.Ordinal);
    }
}
