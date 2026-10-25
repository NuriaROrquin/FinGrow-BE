namespace FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Features.Metrics;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetDepartmentMetricsOverviewQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetDepartmentMetricsOverviewQuery, Result<IReadOnlyList<DepartmentMetricsOverviewResponse>>>
{
    public async Task<Result<IReadOnlyList<DepartmentMetricsOverviewResponse>>> Handle(
        GetDepartmentMetricsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId
            || await companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<IReadOnlyList<DepartmentMetricsOverviewResponse>>(MetricsErrors.NoAutenticado);
        }

        var departmentSnapshots = await snapshots.ListDepartmentsAsync(
            companyId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var response = company.Departments
            .Where(department => department.IsActive)
            .OrderBy(department => department.Name, StringComparer.OrdinalIgnoreCase)
            .Select(department =>
            {
                var latestSnapshot = departmentSnapshots
                    .Where(snapshot => snapshot.DepartmentId == department.Id)
                    .MaxBy(snapshot => snapshot.PeriodStart);

                if (latestSnapshot is null)
                {
                    return new DepartmentMetricsOverviewResponse(
                        department.Id,
                        department.Name,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        false,
                        false,
                        anonymizationPolicy.DepartmentMinimumEmployees,
                        null);
                }

                if (!anonymizationPolicy.CanRevealDepartment(latestSnapshot.Metrics))
                {
                    return new DepartmentMetricsOverviewResponse(
                        department.Id,
                        department.Name,
                        latestSnapshot.PeriodStart,
                        latestSnapshot.Metrics.ActiveEmployees,
                        null,
                        null,
                        null,
                        null,
                        false,
                        true,
                        anonymizationPolicy.DepartmentMinimumEmployees,
                        MetricsAnonymizationPolicy.NotEnoughEmployees.Description);
                }

                var averageScore = anonymizationPolicy.DepartmentAverageScore(latestSnapshot.Metrics);
                FinancialHealthBand? band = averageScore is { } score
                    ? FinancialHealthScale.BandOf(score)
                    : null;

                return new DepartmentMetricsOverviewResponse(
                    department.Id,
                    department.Name,
                    latestSnapshot.PeriodStart,
                    latestSnapshot.Metrics.ActiveEmployees,
                    latestSnapshot.Metrics.ParticipatingEmployees,
                    latestSnapshot.Metrics.ParticipationRate,
                    averageScore,
                    band,
                    band is { } visibleBand && FinancialHealthScale.NeedsAttention(visibleBand),
                    false,
                    anonymizationPolicy.DepartmentMinimumEmployees,
                    null);
            })
            .ToList();

        return Result.Success<IReadOnlyList<DepartmentMetricsOverviewResponse>>(response);
    }
}
