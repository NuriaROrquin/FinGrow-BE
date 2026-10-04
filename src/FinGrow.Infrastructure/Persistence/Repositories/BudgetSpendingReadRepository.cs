namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class BudgetSpendingReadRepository(FinGrowDbContext dbContext) : IBudgetSpendingReadRepository
{
    public async Task<IReadOnlyDictionary<ExpenseCategory, decimal>> GetSpentByCategoryAsync(
        Budget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(budget);

        if (budget.Currency is not { } currency)
        {
            return new Dictionary<ExpenseCategory, decimal>();
        }

        var employeeId = budget.EmployeeId;
        var periodStart = budget.PeriodStart;
        var periodEnd = budget.PeriodEnd;

        var totals = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.EmployeeId == employeeId
                                  && transaction.Type == TransactionType.Expense
                                  && transaction.Status == TransactionStatus.Confirmed
                                  && transaction.ExpenseCategory != null
                                  && transaction.Amount.Currency == currency
                                  && transaction.OccurredOn >= periodStart
                                  && transaction.OccurredOn <= periodEnd)
            .GroupBy(transaction => transaction.ExpenseCategory)
            .Select(group => new
            {
                Category = group.Key,
                Spent = group.Sum(transaction => transaction.Amount.Amount),
            })
            .ToListAsync(cancellationToken);

        return totals.ToDictionary(total => total.Category!.Value, total => total.Spent);
    }
}
