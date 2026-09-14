namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface ITransactionReadRepository
{
    Task<TransactionPage> GetPageAsync(
        int pageNumber,
        int pageSize,
        string? search,
        TransactionType? type,
        CancellationToken cancellationToken = default);
}

public sealed record TransactionPage(
    IReadOnlyList<Transaction> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyDictionary<string, decimal> TotalSpent,
    IReadOnlyDictionary<string, decimal> TotalIncome)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}