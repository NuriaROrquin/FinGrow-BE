namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class MonthlySummaryReadRepository(FinGrowDbContext dbContext) : IMonthlySummaryReadRepository
{
    public async Task<IReadOnlyList<MonthlyMovementTotal>> ListMonthlyTotalsAsync(
        Guid employeeId,
        Currency currency,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var totals = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Amount.Currency == currency
                && transaction.OccurredOn >= fromDate
                && transaction.OccurredOn <= toDate)
            .GroupBy(transaction => new
            {
                transaction.OccurredOn.Year,
                transaction.OccurredOn.Month,
                transaction.Type,
                transaction.ExpenseCategory,
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Type,
                group.Key.ExpenseCategory,
                Total = group.Sum(transaction => transaction.Amount.Amount),
            })
            .ToListAsync(cancellationToken);

        return totals
            .Select(total => new MonthlyMovementTotal(total.Year, total.Month, total.Type, total.ExpenseCategory, total.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<MonthlyActivity>> ListMonthlyActivityAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var activity = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.EmployeeId == employeeId
                && transaction.Status == TransactionStatus.Confirmed)
            .GroupBy(transaction => new { transaction.OccurredOn.Year, transaction.OccurredOn.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Confirmed = group.Count(),
                Incomes = group.Count(transaction => transaction.Type == TransactionType.Income),
            })
            .ToListAsync(cancellationToken);

        return activity
            .Select(month => new MonthlyActivity(month.Year, month.Month, month.Confirmed, month.Incomes > 0))
            .ToList();
    }
}
