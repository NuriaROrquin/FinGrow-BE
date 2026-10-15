namespace FinGrow.Application.Features.Metrics.GetDepartmentMetrics;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetDepartmentMetricsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetDepartmentMetricsQuery, Result<IReadOnlyList<DepartmentMetricsResponse>>>
{
    public async Task<Result<IReadOnlyList<DepartmentMetricsResponse>>> Handle(
        GetDepartmentMetricsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId
            || await companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company
            || company.Departments.All(department => department.Id != request.DepartmentId))
        {
            return Result.Failure<IReadOnlyList<DepartmentMetricsResponse>>(
                Error.NotFound("Metrics.DepartmentNotFound", "No existe el departamento solicitado."));
        }

        var departmentSnapshots = await snapshots.ListDepartmentAsync(
            companyId, request.DepartmentId, request.FromDate, request.ToDate, cancellationToken);

        if (departmentSnapshots.Any(snapshot => !anonymizationPolicy.CanRevealDepartment(snapshot.Metrics)))
        {
            return Result.Failure<IReadOnlyList<DepartmentMetricsResponse>>(
                MetricsAnonymizationPolicy.NotEnoughEmployees);
        }

        return Result.Success<IReadOnlyList<DepartmentMetricsResponse>>(
            departmentSnapshots
                .Select(snapshot => DepartmentMetricsResponse.FromSnapshot(snapshot, company.FiscalYearStartMonth))
                .ToList());
    }
}
