namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;

public interface IEmployeeRepository
{
    Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken);
}
