namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// HU-82: el reporte de un mes que ya se le encoló a una empresa. Hay una sola por empresa y mes, y es
/// lo que impide mandarle dos veces el mismo reporte aunque el trabajo corra todos los días. Apunta al
/// correo de <c>outbound_emails</c> que lo lleva: si ese correo termina abandonado, el trabajo encola
/// otro y la fila pasa a apuntar al nuevo.
/// </summary>
public sealed class MonthlyReportDelivery : AggregateRoot
{
    private MonthlyReportDelivery()
    {
    }

    private MonthlyReportDelivery(
        Guid id,
        Guid companyId,
        MetricsPeriod period,
        Guid outboundEmailId,
        DateTimeOffset queuedAt)
        : base(id)
    {
        CompanyId = companyId;
        PeriodStart = period.Start;
        OutboundEmailId = outboundEmailId;
        CreatedAt = queuedAt;
        QueuedAt = queuedAt;
    }

    public Guid CompanyId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public Guid OutboundEmailId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset QueuedAt { get; private set; }

    public MetricsPeriod Period => MetricsPeriod.FromStart(PeriodStart);

    public static MonthlyReportDelivery Record(
        Guid companyId,
        MetricsPeriod period,
        Guid outboundEmailId,
        DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(period);

        if (companyId == Guid.Empty)
        {
            throw new DomainException("El envío de un reporte mensual siempre pertenece a una empresa.");
        }

        return new MonthlyReportDelivery(
            Guid.CreateVersion7(),
            companyId,
            period,
            EnsureEmail(outboundEmailId),
            queuedAt);
    }

    public void Requeue(Guid outboundEmailId, DateTimeOffset queuedAt)
    {
        if (EnsureEmail(outboundEmailId) == OutboundEmailId)
        {
            throw new DomainException("Volver a encolar el reporte necesita un correo nuevo.");
        }

        OutboundEmailId = outboundEmailId;
        QueuedAt = queuedAt;
    }

    private static Guid EnsureEmail(Guid outboundEmailId) =>
        outboundEmailId == Guid.Empty
            ? throw new DomainException("El envío de un reporte mensual siempre apunta a un correo.")
            : outboundEmailId;
}
