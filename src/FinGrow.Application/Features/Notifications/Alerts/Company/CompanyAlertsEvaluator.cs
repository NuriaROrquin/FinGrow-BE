namespace FinGrow.Application.Features.Notifications.Alerts.Company;

using System.Globalization;
using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;

internal sealed class CompanyAlertsEvaluator(
    ICompanyNotificationSettingRepository settings,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymization,
    Notifier notifier,
    IUnitOfWork unitOfWork)
{
    public async Task<int> EvaluateAsync(Company company, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var companySettings = await settings.ListByCompanyAsync(company.Id, cancellationToken);
        var notifications = 0;

        foreach (var setting in companySettings.Where(setting => setting.IsEnabled && setting.IsDue(now)))
        {
            if (await EvaluateSettingAsync(company, setting, now, cancellationToken) is not null)
            {
                notifications++;
            }

            setting.MarkEvaluated(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return notifications;
    }

    private async Task<Notification?> EvaluateSettingAsync(
        Company company,
        CompanyNotificationSetting setting,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return setting.Type switch
        {
            NotificationType.EmployeeAdded => await EvaluateWorkforceChangeAsync(
                company, setting, increased: true, cancellationToken),
            NotificationType.EmployeeRemoved => await EvaluateWorkforceChangeAsync(
                company, setting, increased: false, cancellationToken),
            NotificationType.FinancialWellbeingLow => await EvaluateFinancialWellbeingAsync(company, setting, now, cancellationToken),
            NotificationType.ParticipationLow => await EvaluateParticipationAsync(company, setting, now, cancellationToken),
            _ => null,
        };
    }

    private async Task<Notification?> EvaluateWorkforceChangeAsync(
        Company company,
        CompanyNotificationSetting setting,
        bool increased,
        CancellationToken cancellationToken)
    {
        var snapshotsForCompany = await snapshots.ListCompanyAsync(company.Id, null, null, cancellationToken);
        var current = snapshotsForCompany
            .Where(snapshot => setting.LastEvaluatedAt is null || snapshot.ComputedAt > setting.LastEvaluatedAt)
            .OrderByDescending(snapshot => snapshot.PeriodStart)
            .FirstOrDefault();

        if (current is null)
        {
            return null;
        }

        var previous = snapshotsForCompany
            .Where(snapshot => snapshot.PeriodStart < current.PeriodStart)
            .OrderByDescending(snapshot => snapshot.PeriodStart)
            .FirstOrDefault();

        if (previous is null
            || (increased
                ? current.Metrics.ActiveEmployees <= previous.Metrics.ActiveEmployees
                : current.Metrics.ActiveEmployees >= previous.Metrics.ActiveEmployees))
        {
            return null;
        }

        var difference = Math.Abs(current.Metrics.ActiveEmployees - previous.Metrics.ActiveEmployees);
        var title = increased ? "Aumento de plantilla" : "Reducción de plantilla";
        var direction = increased ? "aumentó" : "disminuyó";

        return await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Company(company.Id),
                setting.Type,
                title,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"La plantilla activa {direction} en {difference} empleado(s): "
                    + $"de {previous.Metrics.ActiveEmployees} en {previous.Period} a "
                    + $"{current.Metrics.ActiveEmployees} en {current.Period}."),
                $"company-workforce:{setting.Type}:{current.PeriodStart:yyyy-MM-dd}",
                TimeSpan.FromDays(366)),
            cancellationToken);
    }

    private async Task<Notification?> EvaluateFinancialWellbeingAsync(
        Company company,
        CompanyNotificationSetting setting,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var snapshot = await LatestEligibleSnapshotAsync(company.Id, setting, cancellationToken);
        if (snapshot is null
            || snapshot.Metrics.AverageFinancialHealthScore is not { } score
            || score >= setting.Threshold)
        {
            return null;
        }

        return await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Company(company.Id),
                setting.Type,
                "Bienestar financiero a acompañar",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"El bienestar financiero promedio de {snapshot.Period} fue {score:0.#}/100."),
                $"company-wellbeing:{snapshot.PeriodStart:yyyy-MM-dd}",
                TimeSpan.FromDays(366 * 5)),
            cancellationToken);
    }

    private async Task<Notification?> EvaluateParticipationAsync(
        Company company,
        CompanyNotificationSetting setting,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var snapshot = await LatestEligibleSnapshotAsync(company.Id, setting, cancellationToken);
        if (snapshot?.Metrics.ParticipationRate is not { } rate || rate >= setting.Threshold)
        {
            return null;
        }

        return await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Company(company.Id),
                setting.Type,
                "Participación baja",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"La participación de {snapshot.Period} fue {rate:0.#}% sobre {snapshot.Metrics.ActiveEmployees} empleados activos."),
                $"company-participation:{snapshot.PeriodStart:yyyy-MM-dd}",
                TimeSpan.FromDays(366 * 5)),
            cancellationToken);
    }

    private async Task<CompanyMetricsSnapshot?> LatestEligibleSnapshotAsync(
        Guid companyId,
        CompanyNotificationSetting setting,
        CancellationToken cancellationToken)
    {
        var snapshotsForCompany = await snapshots.ListCompanyAsync(companyId, null, null, cancellationToken);
        return snapshotsForCompany
            .Where(snapshot => setting.LastEvaluatedAt is null || snapshot.ComputedAt > setting.LastEvaluatedAt)
            .Where(snapshot => anonymization.CanRevealCompany(snapshot.Metrics))
            .OrderByDescending(snapshot => snapshot.PeriodStart)
            .FirstOrDefault();
    }
}
