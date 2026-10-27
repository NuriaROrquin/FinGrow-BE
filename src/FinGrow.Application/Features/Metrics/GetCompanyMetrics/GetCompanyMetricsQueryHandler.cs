namespace FinGrow.Application.Features.Metrics.GetCompanyMetrics;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class GetCompanyMetricsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
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

        if (await companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<IReadOnlyList<CompanyMetricsResponse>>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var companySnapshots = await snapshots.ListCompanyAsync(
            companyId, request.FromDate, request.ToDate, cancellationToken);

        return Result.Success(
            CompanyMetricsSeries.Build(companySnapshots, company.FiscalYearStartMonth, anonymizationPolicy));
    }
}
