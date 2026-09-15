namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly FinGrowDbContext _dbContext;

    public EmployeeRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        _dbContext.Set<Employee>().SingleOrDefaultAsync(employee => employee.Email == email, cancellationToken);
}
