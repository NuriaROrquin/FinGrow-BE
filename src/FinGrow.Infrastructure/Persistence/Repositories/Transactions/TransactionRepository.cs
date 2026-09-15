namespace FinGrow.Infrastructure.Persistence.Repositories.Transactions;

using Domain.Entities;
using FinGrow.Domain.Repositories.Transactions;
using Microsoft.EntityFrameworkCore;

public class TransactionRepository(FinGrowDbContext dbContext) : ITransactionRepository
{
    // Comitea en el mismo paso: no hay un IUnitOfWork separado en el handler ni en el servicio,
    // asi que el repositorio es el unico responsable de persistir lo que agrega.
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Transactions.FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.Transactions
            .Where(transaction => transaction.EmployeeId == employeeId)
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .ToListAsync(cancellationToken);
}
