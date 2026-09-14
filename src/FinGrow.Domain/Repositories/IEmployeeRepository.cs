namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
