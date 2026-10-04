namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;

public interface IMetricsSnapshotRepository
{
    Task<IReadOnlySet<DateOnly>> ListCompanyPeriodStartsAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<CompanyMetricsSnapshot?> FindCompanyAsync(Guid companyId, DateOnly periodStart, CancellationToken cancellationToken = default);

    Task<DepartmentMetricsSnapshot?> FindDepartmentAsync(Guid departmentId, DateOnly periodStart, CancellationToken cancellationToken = default);

    void Add(CompanyMetricsSnapshot snapshot);

    void Add(DepartmentMetricsSnapshot snapshot);
}
