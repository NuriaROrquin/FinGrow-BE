namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class CompanyRepository : ICompanyRepository
{
    private readonly FinGrowDbContext _dbContext;

    public CompanyRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Companies.FirstOrDefaultAsync(company => company.Id == id, cancellationToken);

    public Task<Company?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        _dbContext.Set<Company>().SingleOrDefaultAsync(company => company.Email == email, cancellationToken);
}

