namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly FinGrowDbContext _dbContext;

    public EmployeeRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Employees.FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        _dbContext.Set<Employee>().SingleOrDefaultAsync(employee => employee.Email == email, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountByDepartmentAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Employees
            .Where(employee => employee.CompanyId == companyId && employee.DepartmentId != null)
            .GroupBy(employee => employee.DepartmentId!.Value)
            .Select(group => new { DepartmentId = group.Key, Employees = group.Count() })
            .ToDictionaryAsync(row => row.DepartmentId, row => row.Employees, cancellationToken);

    public async Task<IReadOnlyList<Employee>> ListByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.CompanyId == companyId)
            .OrderBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);

    public void Add(Employee employee) => _dbContext.Employees.Add(employee);

    public void Remove(Employee employee) => _dbContext.Employees.Remove(employee);
}
