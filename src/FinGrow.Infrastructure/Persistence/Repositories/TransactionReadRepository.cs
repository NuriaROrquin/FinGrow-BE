namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionReadRepository : ITransactionReadRepository
{
    private readonly FinGrowDbContext _dbContext;

    public TransactionReadRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task<TransactionSummary> GetSummaryAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var confirmedQuery = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.EmployeeId == employeeId)
            .Where(transaction => transaction.Status == TransactionStatus.Confirmed);

        var totalTransactions = await confirmedQuery.CountAsync(cancellationToken);
        var totalIncomeTransactions = await confirmedQuery.CountAsync(
            transaction => transaction.Type == TransactionType.Income, cancellationToken);
        var totalExpenseTransactions = await confirmedQuery.CountAsync(
            transaction => transaction.Type == TransactionType.Expense, cancellationToken);

        var totalIncomeArs = await GetTotalAsync(confirmedQuery, TransactionType.Income, Currency.ARS, cancellationToken);
        var totalIncomeUsd = await GetTotalAsync(confirmedQuery, TransactionType.Income, Currency.USD, cancellationToken);
        var totalExpenseArs = await GetTotalAsync(confirmedQuery, TransactionType.Expense, Currency.ARS, cancellationToken);
        var totalExpenseUsd = await GetTotalAsync(confirmedQuery, TransactionType.Expense, Currency.USD, cancellationToken);

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

        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TransactionPage(items, pageNumber, pageSize, totalCount);
    }

    private static Task<decimal> GetTotalAsync(
        IQueryable<Transaction> query,
        TransactionType type,
        Currency currency,
        CancellationToken cancellationToken) =>
        query
            .Where(transaction => transaction.Type == type && transaction.Amount.Currency == currency)
            .SumAsync(transaction => transaction.Amount.Amount, cancellationToken);
}
