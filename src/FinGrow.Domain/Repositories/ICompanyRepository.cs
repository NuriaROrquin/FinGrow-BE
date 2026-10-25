namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Company?> GetWithDepartmentsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> ListActiveAsync(CancellationToken cancellationToken = default);
}
