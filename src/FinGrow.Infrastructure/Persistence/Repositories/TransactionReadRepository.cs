namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionReadRepository : ITransactionReadRepository
{
    private readonly FinGrowDbContext _dbContext;

    public TransactionReadRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task<TransactionSummary> GetSummaryAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var confirmedQuery = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.EmployeeId == employeeId)
            .Where(transaction => transaction.Status == TransactionStatus.Confirmed);

        if (fromDate.HasValue)
        {
            confirmedQuery = confirmedQuery.Where(transaction => transaction.OccurredOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            confirmedQuery = confirmedQuery.Where(transaction => transaction.OccurredOn <= toDate.Value);
        }

        var totals = await confirmedQuery
            .GroupBy(transaction => new { transaction.Type, transaction.Amount.Currency })
            .Select(group => new TypeCurrencyTotal(
                group.Key.Type,
                group.Key.Currency,
                group.Count(),
                group.Sum(transaction => transaction.Amount.Amount)))
            .ToListAsync(cancellationToken);

        var totalTransactions = totals.Sum(total => total.Count);
        var totalIncomeTransactions = totals
            .Where(total => total.Type == TransactionType.Income)
            .Sum(total => total.Count);
        var totalExpenseTransactions = totals
            .Where(total => total.Type == TransactionType.Expense)
            .Sum(total => total.Count);

        var totalIncomeArs = GetTotal(totals, TransactionType.Income, Currency.ARS);
        var totalIncomeUsd = GetTotal(totals, TransactionType.Income, Currency.USD);
        var totalExpenseArs = GetTotal(totals, TransactionType.Expense, Currency.ARS);
        var totalExpenseUsd = GetTotal(totals, TransactionType.Expense, Currency.USD);

        return new TransactionSummary(
            totalTransactions,
            totalExpenseTransactions,
            totalIncomeTransactions,
            totalIncomeArs,
            totalIncomeUsd,
            totalExpenseArs,
            totalExpenseUsd);
    }
    

    public async Task<TransactionPage> GetPageAsync(
        Guid employeeId,
        int pageNumber,
        int pageSize,
        string? search,
        TransactionType? type,
        TransactionStatus? status,
        ExpenseCategory? expenseCategory,
        IncomeCategory? incomeCategory,
        PaymentMethod? paymentMethod,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.EmployeeId == employeeId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            filteredQuery = filteredQuery.Where(transaction =>
                EF.Functions.ILike(transaction.Description, pattern)
                || (transaction.ExternalReference != null
                    && EF.Functions.ILike(transaction.ExternalReference, pattern)));
        }

        if (type.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.Type == type.Value);
        }

            if (status.HasValue)
            {
                filteredQuery = filteredQuery.Where(transaction => transaction.Status == status.Value);
            }

        if (expenseCategory.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.ExpenseCategory == expenseCategory.Value);
        }

        if (incomeCategory.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.IncomeCategory == incomeCategory.Value);
        }

        if (paymentMethod.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.PaymentMethod == paymentMethod.Value);
        }

        if (fromDate.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.OccurredOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filteredQuery = filteredQuery.Where(transaction => transaction.OccurredOn <= toDate.Value);
        }

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TransactionPage(items, pageNumber, pageSize, totalCount);
    }

    private static decimal GetTotal(IEnumerable<TypeCurrencyTotal> totals, TransactionType type, Currency currency) =>
        totals.FirstOrDefault(total => total.Type == type && total.Currency == currency)?.Total ?? 0m;

    private sealed record TypeCurrencyTotal(TransactionType Type, Currency Currency, int Count, decimal Total);
}
