namespace FinGrow.Infrastructure.Persistence.Repositories;

using Domain.Entities;
using Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class BudgetRepository(FinGrowDbContext dbContext) : IBudgetRepository
{
    public void Add(Budget budget) => dbContext.Budgets.Add(budget);

    public void Remove(Budget budget) => dbContext.Budgets.Remove(budget);

    public Task<bool> ExistsForPeriodAsync(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        dbContext.Budgets.AnyAsync(
            budget => budget.EmployeeId == employeeId
                      && budget.Period == period
                      && budget.PeriodStart == periodStart,
            cancellationToken);

    public Task<Budget?> FindForPeriodAsync(
        Guid employeeId,
        BudgetPeriod period,
        DateOnly periodStart,
        CancellationToken cancellationToken = default) =>
        dbContext.Budgets.FirstOrDefaultAsync(
            budget => budget.EmployeeId == employeeId
                      && budget.Period == period
                      && budget.PeriodStart == periodStart,
            cancellationToken);

    public async Task<IReadOnlyList<Budget>> ListMonthlyAsync(
        Guid employeeId,
        DateOnly fromPeriodStart,
        DateOnly toPeriodStart,
        CancellationToken cancellationToken = default) =>
        await dbContext.Budgets
            .AsNoTracking()
            .Where(budget => budget.EmployeeId == employeeId
                             && budget.Period == BudgetPeriod.Monthly
                             && budget.PeriodStart >= fromPeriodStart
                             && budget.PeriodStart <= toPeriodStart)
            .OrderBy(budget => budget.PeriodStart)
            .ToListAsync(cancellationToken);
}
