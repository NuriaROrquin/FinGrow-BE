namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

internal sealed class TransactionReadRepository : ITransactionReadRepository
{
    private readonly FinGrowDbContext _dbContext;

    public TransactionReadRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task<TransactionPage> GetPageAsync(
        int pageNumber,
        int pageSize,
        string? search,
        TransactionType? type,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = _dbContext.Transactions
            .AsNoTracking();

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
        var totalSpent = await GetTotalsByCurrencyAsync(
            filteredQuery,
            TransactionType.Expense,
            cancellationToken);
        var totalIncome = await GetTotalsByCurrencyAsync(
            filteredQuery,
            TransactionType.Income,
            cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(transaction => transaction.OccurredOn)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TransactionPage(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalSpent,
            totalIncome);
    }

    private static async Task<IReadOnlyDictionary<string, decimal>> GetTotalsByCurrencyAsync(
        IQueryable<Transaction> query,
        TransactionType type,
        CancellationToken cancellationToken) =>
        (await query
            .Where(transaction => transaction.Type == type)
            .Where(transaction => transaction.Status == TransactionStatus.Confirmed)
            .GroupBy(transaction => transaction.Amount.Currency)
            .Select(group => new
            {
                Currency = group.Key,
                Total = group.Sum(transaction => transaction.Amount.Amount)
            })
            .ToListAsync(cancellationToken))
        .ToDictionary(item => item.Currency.ToString(), item => item.Total);
}