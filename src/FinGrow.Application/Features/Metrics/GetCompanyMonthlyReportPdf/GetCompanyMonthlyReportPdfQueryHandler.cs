namespace FinGrow.Application.Features.Metrics.GetCompanyMonthlyReportPdf;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

/// <summary>Siempre el último mes con foto, el mismo que miran las tendencias y el resto del panel.</summary>
internal sealed class GetCompanyMonthlyReportPdfQueryHandler(
    ICurrentUser currentUser,
    ICompanyRepository companies,
    IMetricsSnapshotRepository snapshots,
    CompanyReportPdfBuilder pdfs,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyMonthlyReportPdfQuery, Result<CompanyReportPdfFile>>
{
    public async Task<Result<CompanyReportPdfFile>> Handle(
        GetCompanyMonthlyReportPdfQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.CompanyId is not { } companyId)
        {
            return Result.Failure<CompanyReportPdfFile>(
                Error.Forbidden("Metrics.Unauthenticated", "Solo una empresa puede consultar sus metricas."));
        }

        if (await companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyReportPdfFile>(
                Error.NotFound("Metrics.CompanyNotFound", "No existe la empresa solicitada."));
        }

        var periods = await snapshots.ListCompanyPeriodStartsAsync(companyId, cancellationToken);

        if (periods.Count == 0
            || await pdfs.BuildAsync(company, MetricsPeriod.FromStart(periods.Max()), clock.UtcNow, cancellationToken) is not { } pdf)
        {
            return Result.Failure<CompanyReportPdfFile>(
                Error.NotFound("Metrics.NoClosedMonth", "Todavía no hay meses cerrados para exportar."));
        }

        return Result.Success(new CompanyReportPdfFile(pdf.FileName, pdf.Content));
    }
}
