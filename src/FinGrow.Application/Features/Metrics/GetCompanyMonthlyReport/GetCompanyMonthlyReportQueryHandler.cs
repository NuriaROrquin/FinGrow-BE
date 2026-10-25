namespace FinGrow.Application.Features.Metrics.GetCompanyMonthlyReport;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.MonthlyReport;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

/// <summary>
/// El reporte que exporta el panel (HU-65): siempre el último mes con foto, el mismo que miran las
/// tendencias, las preocupaciones y las recomendaciones del panel.
/// </summary>
internal sealed class GetCompanyMonthlyReportQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    CompanyMonthlyReportBuilder reports,
    MetricsAnonymizationPolicy anonymizationPolicy)
    : IRequestHandler<GetCompanyMonthlyReportQuery, Result<CompanyMonthlyReportResponse>>
{
    public async Task<Result<CompanyMonthlyReportResponse>> Handle(
        GetCompanyMonthlyReportQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyMonthlyReportResponse>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        if (await companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyMonthlyReportResponse>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var withoutPeriod = CompanyMonthlyReportResponse.WithoutPeriod(
            company.Name,
            anonymizationPolicy.CompanyMinimumEmployees);
        var periods = await snapshots.ListCompanyPeriodStartsAsync(companyId, cancellationToken);

        if (periods.Count == 0)
        {
            return Result.Success(withoutPeriod);
        }

        var report = await reports.BuildAsync(company, MetricsPeriod.FromStart(periods.Max()), cancellationToken);

        return Result.Success(report ?? withoutPeriod);
    }
}
