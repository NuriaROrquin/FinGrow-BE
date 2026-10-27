namespace FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;

using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

/// <summary>
/// Decide si a una empresa le toca el reporte del último mes cerrado y, si le toca, lo encola en
/// <c>outbound_emails</c> junto con su <see cref="MonthlyReportDelivery"/> en el mismo <c>SaveChanges</c>:
/// o quedan los dos o no queda ninguno, y la corrida siguiente lo vuelve a intentar.
/// </summary>
internal sealed class CompanyMonthlyReportMailer(
    ICompanyNotificationSettingRepository settings,
    IMonthlyReportDeliveryRepository deliveries,
    IOutboundEmailRepository outboundEmails,
    CompanyReportPdfBuilder pdfs,
    IOptions<CompanyMonthlyReportOptions> options,
    IUnitOfWork unitOfWork)
{
    public async Task<MonthlyReportMailOutcome> SendAsync(
        Company company,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);

        var setting = (await settings.ListByCompanyAsync(company.Id, cancellationToken))
            .FirstOrDefault(candidate => candidate.Type == NotificationType.MonthlyReport);

        if (setting is not { IsEnabled: true, EnabledAt: { } enabledAt })
        {
            return MonthlyReportMailOutcome.NotSubscribed;
        }

        var period = MetricsPeriod.LastClosedMonth(ArgentinaTime.DateOf(now));

        // "Desde el próximo cierre": un mes que ya había cerrado cuando la empresa prendió el
        // switch no se manda, aunque todavía no se le haya mandado nada.
        if (enabledAt >= period.EndInstantExclusive)
        {
            return MonthlyReportMailOutcome.ClosedBeforeSubscribing;
        }

        var delivery = await deliveries.FindAsync(company.Id, period.Start, cancellationToken);

        if (delivery is not null
            && await outboundEmails.GetByIdAsync(delivery.OutboundEmailId, cancellationToken) is { } queued
            && queued.Status != OutboundEmailStatus.Abandoned)
        {
            return MonthlyReportMailOutcome.AlreadyQueued;
        }

        // Sin foto (metrics-snapshot no corrió o le faltó el MEP) se espera a la corrida siguiente.
        if (await pdfs.BuildAsync(company, period, now, cancellationToken) is not { } pdf)
        {
            return MonthlyReportMailOutcome.WaitingForSnapshot;
        }

        var email = CompanyMonthlyReportEmail.Queue(company, pdf, options.Value.PanelUrl, now);
        outboundEmails.Add(email);

        if (delivery is null)
        {
            deliveries.Add(MonthlyReportDelivery.Record(company.Id, period, email.Id, now));
        }
        else
        {
            delivery.Requeue(email.Id, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return delivery is null ? MonthlyReportMailOutcome.Queued : MonthlyReportMailOutcome.Requeued;
    }
}

internal enum MonthlyReportMailOutcome
{
    NotSubscribed = 1,
    ClosedBeforeSubscribing = 2,
    AlreadyQueued = 3,
    WaitingForSnapshot = 4,
    Queued = 5,
    Requeued = 6,
}
