namespace FinGrow.Domain.Repositories;

using Entities;

public interface IInvestmentRepository
{
    void Add(Investment investment);

    void Remove(Investment investment);

    Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
