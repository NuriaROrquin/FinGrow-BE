namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> CountByDepartmentAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Employee>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Employee>> ListByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Employee>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    void Add(Employee employee);

    void Remove(Employee employee);
}
