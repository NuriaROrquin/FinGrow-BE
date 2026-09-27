namespace FinGrow.Domain.Repositories;

using Entities;

public interface IInvestmentRepository
{
    void Add(Investment investment);

    Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
