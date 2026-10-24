namespace FinGrow.Application.Features.Metrics.GetCompanyDepartmentMetrics;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.GetDepartmentMetrics;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetCompanyDepartmentMetricsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetCompanyDepartmentMetricsQuery, Result<IReadOnlyList<CompanyDepartmentMetricsResponse>>>
{
    public async Task<Result<IReadOnlyList<CompanyDepartmentMetricsResponse>>> Handle(
        GetCompanyDepartmentMetricsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId
            || await companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<IReadOnlyList<CompanyDepartmentMetricsResponse>>(MetricsErrors.NoAutenticado);
        }

        var departmentSnapshots = await snapshots.ListDepartmentsAsync(
            companyId, request.FromDate, request.ToDate, cancellationToken);

        var responses = company.Departments
            .OrderBy(department => department.Name, StringComparer.OrdinalIgnoreCase)
            .Select(department =>
            {
                var snapshotsForDepartment = departmentSnapshots
                    .Where(snapshot => snapshot.DepartmentId == department.Id)
                    .ToList();
                var visibleSnapshots = snapshotsForDepartment
                    .Where(snapshot => anonymizationPolicy.CanRevealDepartment(snapshot.Metrics))
                    .ToList();
                var hasHiddenMetrics = snapshotsForDepartment.Count != visibleSnapshots.Count;

                return new CompanyDepartmentMetricsResponse(
                    department.Id,
                    department.Name,
                    department.IsActive,
                    visibleSnapshots
                        .Select(snapshot => DepartmentMetricsResponse.FromSnapshot(
                            snapshot,
                            company.FiscalYearStartMonth,
                            anonymizationPolicy.DepartmentAverageScore(snapshot.Metrics)))
                        .ToList(),
                    hasHiddenMetrics,
                    anonymizationPolicy.DepartmentMinimumEmployees,
                    hasHiddenMetrics ? MetricsAnonymizationPolicy.NotEnoughEmployees.Description : null);
            })
            .ToList();

        return Result.Success<IReadOnlyList<CompanyDepartmentMetricsResponse>>(responses);
    }
}
