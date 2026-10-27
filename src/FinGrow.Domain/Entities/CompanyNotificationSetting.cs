namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public sealed class CompanyNotificationSetting : AggregateRoot
{
    private CompanyNotificationSetting()
    {
    }

    private CompanyNotificationSetting(
        Guid id,
        Guid companyId,
        NotificationType type,
        bool isEnabled,
        NotificationCadence cadence,
        decimal? threshold,
        DateTimeOffset updatedAt)
        : base(id)
    {
        CompanyId = companyId;
        Type = type;
        IsEnabled = isEnabled;
        Cadence = cadence;
        Threshold = threshold;
        UpdatedAt = updatedAt;
        EnabledAt = isEnabled ? updatedAt : null;
    }

    public Guid CompanyId { get; private set; }

    public NotificationType Type { get; private set; }

    public bool IsEnabled { get; private set; }

    public NotificationCadence Cadence { get; private set; }

    public decimal? Threshold { get; private set; }

    public DateTimeOffset? LastEvaluatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Cuándo pasó de apagada a prendida; <c>null</c> mientras está apagada. No es <see cref="UpdatedAt"/>:
    /// guardar otra vez la misma preferencia lo pisaría, y el reporte mensual (HU-82) solo manda los
    /// meses que cerraron después de que la empresa lo pidió.
    /// </summary>
    public DateTimeOffset? EnabledAt { get; private set; }

    public static CompanyNotificationSetting Create(
        Guid companyId,
        NotificationType type,
        bool isEnabled,
        NotificationCadence cadence,
        DateTimeOffset updatedAt,
        decimal? threshold = null)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("La preferencia de alerta siempre pertenece a una empresa.");
        }

        EnsureCompanyType(type);
        EnsureCadence(type, cadence);
        threshold = ResolveThreshold(type, threshold);

        return new CompanyNotificationSetting(Guid.CreateVersion7(), companyId, type, isEnabled, cadence, threshold, updatedAt);
    }

    public void Change(bool isEnabled, NotificationCadence cadence, DateTimeOffset updatedAt, decimal? threshold = null)
    {
        EnsureCadence(Type, cadence);

        if (!isEnabled)
        {
            EnabledAt = null;
        }
        else if (!IsEnabled)
        {
            EnabledAt = updatedAt;
        }

        IsEnabled = isEnabled;
        Cadence = cadence;
        Threshold = ResolveThreshold(Type, threshold ?? Threshold);
        UpdatedAt = updatedAt;
    }

    public bool IsDue(DateTimeOffset now) =>
        LastEvaluatedAt is null || now >= LastEvaluatedAt.Value.AddMonths(1);

    public void MarkEvaluated(DateTimeOffset evaluatedAt) => LastEvaluatedAt = evaluatedAt;

    public static bool IsCompanyType(NotificationType type) =>
        IsAlertType(type) || type == NotificationType.MonthlyReport;

    /// <summary>
    /// Lo que evalúa <c>company-alerts</c> y termina en la bandeja. <see cref="NotificationType.MonthlyReport"/>
    /// es una preferencia de la empresa pero no una alerta: es un mail que manda <c>company-monthly-report</c>.
    /// </summary>
    public static bool IsAlertType(NotificationType type) =>
        type is NotificationType.FinancialWellbeingLow
            or NotificationType.ParticipationLow
            or NotificationType.EmployeeAdded
            or NotificationType.EmployeeRemoved;

    private static void EnsureCompanyType(NotificationType type)
    {
        if (!IsCompanyType(type))
        {
            throw new DomainException("Ese tipo de alerta no se puede configurar para una empresa.");
        }
    }

    private static void EnsureCadence(NotificationType type, NotificationCadence cadence)
    {
        if (!Enum.IsDefined(cadence) || cadence != NotificationCadence.Monthly)
        {
            throw new DomainException("La frecuencia elegida no es válida para este tipo de alerta.");
        }
    }

    private static decimal? ResolveThreshold(NotificationType type, decimal? threshold)
    {
        if (type is not (NotificationType.FinancialWellbeingLow or NotificationType.ParticipationLow))
        {
            if (threshold is not null)
            {
                throw new DomainException("Solo bienestar y participación admiten un umbral.");
            }

            return null;
        }

        var value = threshold ?? (type == NotificationType.FinancialWellbeingLow ? 60m : 50m);

        if (value is < 1m or > 100m)
        {
            throw new DomainException("El umbral tiene que estar entre 1 y 100.");
        }

        return value;
    }
}
