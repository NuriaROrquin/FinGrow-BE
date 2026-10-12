namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class FinancialHealthScoreRepository(FinGrowDbContext dbContext) : IFinancialHealthScoreRepository
{
    public async Task<IReadOnlyList<FinancialHealthScore>> ListByCompanyAsync(
        Guid companyId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        await dbContext.FinancialHealthScores
            .Where(score => score.PeriodStart == periodStart
                && dbContext.Employees.Any(employee => employee.Id == score.EmployeeId && employee.CompanyId == companyId))
            .ToListAsync(cancellationToken);

    public void Add(FinancialHealthScore score) => dbContext.FinancialHealthScores.Add(score);

    public void Remove(FinancialHealthScore score) => dbContext.FinancialHealthScores.Remove(score);
}
