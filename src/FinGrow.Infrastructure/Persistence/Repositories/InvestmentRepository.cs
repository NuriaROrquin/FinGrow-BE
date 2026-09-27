namespace FinGrow.Infrastructure.Persistence.Repositories;

using Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class InvestmentRepository(FinGrowDbContext dbContext) : IInvestmentRepository
{
    public void Add(Investment investment) => dbContext.Investments.Add(investment);

    public void Remove(Investment investment) => dbContext.Investments.Remove(investment);

    public Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Investments.FirstOrDefaultAsync(investment => investment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.Investments
            .Where(investment => investment.EmployeeId == employeeId)
            .OrderByDescending(investment => investment.PurchasedOn)
            .ThenByDescending(investment => investment.CreatedAt)
            .ToListAsync(cancellationToken);
}
