namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class FinancialHealthReadRepository(FinGrowDbContext dbContext) : IFinancialHealthReadRepository
{
    public async Task<IReadOnlyList<EmployeeCurrencyTotals>> ListMovementTotalsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        MetricsPeriod period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employeeIds);
        ArgumentNullException.ThrowIfNull(period);

        var start = period.Start;
        var end = period.End;
        var recentStart = start.AddMonths(-(FinancialHealthFactors.SpendingAverageMonths - 1));

        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => employeeIds.Contains(transaction.EmployeeId)
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.OccurredOn <= end)
            .GroupBy(transaction => new { transaction.EmployeeId, transaction.Amount.Currency })
            .Select(group => new EmployeeCurrencyTotals(
                group.Key.EmployeeId,
                group.Key.Currency,
                group.Min(transaction => transaction.OccurredOn),
                group.Sum(transaction => transaction.Type == TransactionType.Income
                    && transaction.OccurredOn >= start
                        ? transaction.Amount.Amount
                        : 0m),
                group.Sum(transaction => transaction.Type == TransactionType.Expense
                    && transaction.ExpenseCategory != ExpenseCategory.AhorroInversion
                    && transaction.OccurredOn >= start
                        ? transaction.Amount.Amount
                        : 0m),
                group.Sum(transaction => transaction.Type == TransactionType.Income
                    ? transaction.Amount.Amount
                    : 0m),
                group.Sum(transaction => transaction.Type == TransactionType.Expense
                    && transaction.ExpenseCategory != ExpenseCategory.AhorroInversion
                        ? transaction.Amount.Amount
                        : 0m),
                group.Sum(transaction => transaction.Type == TransactionType.Expense
                    && transaction.ExpenseCategory != ExpenseCategory.AhorroInversion
                    && transaction.OccurredOn >= recentStart
                        ? transaction.Amount.Amount
                        : 0m)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeBudgetLimit>> ListBudgetLimitsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        MetricsPeriod period,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employeeIds);
        ArgumentNullException.ThrowIfNull(period);

        var start = period.Start;
        var end = period.End;

        var limits = await dbContext.Budgets
            .AsNoTracking()
            .Where(budget => employeeIds.Contains(budget.EmployeeId)
                && budget.Period == BudgetPeriod.Monthly
                && budget.PeriodStart == start)
            .SelectMany(budget => budget.Limits, (budget, limit) => new
            {
                budget.EmployeeId,
                limit.Category,
                limit.Limit.Amount,
                limit.Limit.Currency,
            })
            .Select(limit => new
            {
                limit.EmployeeId,
                limit.Amount,
                limit.Currency,
                Spent = dbContext.Transactions
                    .Where(transaction => transaction.EmployeeId == limit.EmployeeId
                        && transaction.Type == TransactionType.Expense
                        && transaction.Status == TransactionStatus.Confirmed
                        && transaction.ExpenseCategory == limit.Category
                        && transaction.Amount.Currency == limit.Currency
                        && transaction.OccurredOn >= start
                        && transaction.OccurredOn <= end)
                    .Sum(transaction => transaction.Amount.Amount),
            })
            .ToListAsync(cancellationToken);

        return limits
            .Select(limit => new EmployeeBudgetLimit(
                limit.EmployeeId,
                Money.From(limit.Amount, limit.Currency),
                Money.From(limit.Spent, limit.Currency)))
            .ToList();
    }
}
