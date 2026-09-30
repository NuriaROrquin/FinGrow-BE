namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class MetricsSnapshotRepository : IMetricsSnapshotRepository
{
    private readonly FinGrowDbContext _dbContext;

    public MetricsSnapshotRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlySet<DateOnly>> ListCompanyPeriodStartsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var periodStarts = await _dbContext.CompanyMetricsSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.CompanyId == companyId)
            .Select(snapshot => snapshot.PeriodStart)
            .ToListAsync(cancellationToken);

        return periodStarts.ToHashSet();
    }

    public Task<CompanyMetricsSnapshot?> FindCompanyAsync(Guid companyId, DateOnly periodStart, CancellationToken cancellationToken = default) =>
        _dbContext.CompanyMetricsSnapshots.SingleOrDefaultAsync(
            snapshot => snapshot.CompanyId == companyId && snapshot.PeriodStart == periodStart,
            cancellationToken);

    public Task<DepartmentMetricsSnapshot?> FindDepartmentAsync(Guid departmentId, DateOnly periodStart, CancellationToken cancellationToken = default) =>
        _dbContext.DepartmentMetricsSnapshots.SingleOrDefaultAsync(
            snapshot => snapshot.DepartmentId == departmentId && snapshot.PeriodStart == periodStart,
            cancellationToken);

    public void Add(CompanyMetricsSnapshot snapshot) => _dbContext.CompanyMetricsSnapshots.Add(snapshot);

    public void Add(DepartmentMetricsSnapshot snapshot) => _dbContext.DepartmentMetricsSnapshots.Add(snapshot);
}
