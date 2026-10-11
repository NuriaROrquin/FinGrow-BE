namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IFinancialHealthScoreRepository
{
    Task<IReadOnlyList<FinancialHealthScore>> ListByCompanyAsync(
        Guid companyId,
        DateOnly periodStart,
        CancellationToken cancellationToken = default);

    void Add(FinancialHealthScore score);

    void Remove(FinancialHealthScore score);
}
