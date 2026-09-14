namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly FinGrowDbContext _dbContext;

    public EmployeeRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Employees.FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);
}
