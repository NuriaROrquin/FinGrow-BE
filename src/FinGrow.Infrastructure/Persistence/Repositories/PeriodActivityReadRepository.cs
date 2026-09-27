namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class PeriodActivityReadRepository : IPeriodActivityReadRepository
{
    private readonly FinGrowDbContext _dbContext;

    public PeriodActivityReadRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<EmployeePeriodActivity>> ListByCompanyAsync(
        Guid companyId,
        MetricsPeriod period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(period);

        var start = period.Start;
        var end = period.End;
        var startInstant = period.StartInstant;
        var endExclusive = period.EndInstantExclusive;
        var yearStart = new DateOnly(start.Year, 1, 1);

        return await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.CompanyId == companyId && employee.IsActive && employee.CreatedAt < endExclusive)
            .OrderBy(employee => employee.Id)
            .Select(employee => new EmployeePeriodActivity(
                employee.Id,
                employee.DepartmentId,
                _dbContext.Transactions.Count(transaction =>
                    transaction.EmployeeId == employee.Id
                    && transaction.Status == TransactionStatus.Confirmed
                    && transaction.OccurredOn >= start
                    && transaction.OccurredOn <= end),
                _dbContext.Goals
                    .Where(goal => goal.EmployeeId == employee.Id)
                    .SelectMany(goal => goal.Contributions)
                    .Count(contribution => contribution.ContributedOn >= start && contribution.ContributedOn <= end),
                _dbContext.Budgets.Any(budget =>
                    budget.EmployeeId == employee.Id
                    && (budget.PeriodStart == start
                        || (budget.Period != BudgetPeriod.Monthly && budget.PeriodStart == yearStart))),
                _dbContext.Goals.Any(goal =>
                    goal.EmployeeId == employee.Id
                    && goal.CreatedAt < endExclusive
                    && (goal.Status == GoalStatus.Active
                        || (goal.Status == GoalStatus.Achieved && goal.AchievedAt >= endExclusive))),
                _dbContext.Goals.Count(goal =>
                    goal.EmployeeId == employee.Id
                    && goal.AchievedAt >= startInstant
                    && goal.AchievedAt < endExclusive),
                _dbContext.EmployeeIntegrations.Any(integration =>
                    integration.EmployeeId == employee.Id
                    && integration.LinkedAt < endExclusive)))
            .ToListAsync(cancellationToken);
    }
}
