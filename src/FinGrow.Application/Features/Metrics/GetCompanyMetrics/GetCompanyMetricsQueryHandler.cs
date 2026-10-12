namespace FinGrow.Application.Features.Metrics.GetCompanyMetrics;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetCompanyMetricsQueryHandler(
    ICurrentUser currentUser,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetCompanyMetricsQuery, Result<IReadOnlyList<CompanyMetricsResponse>>>
{
    public async Task<Result<IReadOnlyList<CompanyMetricsResponse>>> Handle(
        GetCompanyMetricsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<IReadOnlyList<CompanyMetricsResponse>>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        var companySnapshots = await snapshots.ListCompanyAsync(
            companyId, request.FromDate, request.ToDate, cancellationToken);

        if (companySnapshots.Any(snapshot => !anonymizationPolicy.CanRevealCompany(snapshot.Metrics)))
        {
            return Result.Failure<IReadOnlyList<CompanyMetricsResponse>>(
                MetricsAnonymizationPolicy.NotEnoughEmployees);
        }

        return Result.Success<IReadOnlyList<CompanyMetricsResponse>>(
            companySnapshots.Select(CompanyMetricsResponse.FromSnapshot).ToList());
    }
}
