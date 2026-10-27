namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetCompanyInsightsQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy,
    CompanyInsightsBuilder insights)
    : IRequestHandler<GetCompanyInsightsQuery, Result<CompanyInsightsResponse>>
{
    public async Task<Result<CompanyInsightsResponse>> Handle(
        GetCompanyInsightsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyInsightsResponse>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        var company = await companies.GetByIdAsync(companyId, cancellationToken);

        if (company is null)
        {
            return Result.Failure<CompanyInsightsResponse>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var periods = await snapshots.ListCompanyPeriodStartsAsync(companyId, cancellationToken);

        if (periods.Count == 0)
        {
            return Result.Success(CompanyInsightsResponse.WithoutPeriod(anonymizationPolicy.CompanyMinimumEmployees));
        }

        // Los puntajes los calcula metrics-snapshot junto con la foto: el último mes fotografiado
        // es el último mes cerrado que se puede medir.
        return Result.Success(await insights.BuildAsync(company, periods.Max(), cancellationToken));
    }
}
