namespace FinGrow.Application.Features.Notifications.Alerts.InvestmentAlerts;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class InvestmentAlertsJob : IScheduledJob
{
    public const string JobName = "investment-alerts";

    internal const string ValueChangedTitle = "Cambió el valor de tu portafolio";
    internal const string MaturingTitle = "Una inversión está por vencer";

    internal const decimal ChangeThresholdPercent = 5m;

    // Un valor sin valuaciones nuevas desde hace mas que esto no es "lo que cambio": no se avisa.
    internal const int MaxValuationAgeDays = 3;

    internal static readonly int[] MaturityMilestones = { 1, 7 };

    // La clave ya lleva la fecha de la valuacion; la ventana solo evita repetir si el trabajo corre dos veces.
    internal static readonly TimeSpan ValueChangedDeduplicationWindow = TimeSpan.FromDays(30);

    internal static readonly TimeSpan MaturityDeduplicationWindow = TimeSpan.FromDays(366 * 10);

    private static readonly NumberFormatInfo ArgentineNumbers = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
    };

    private readonly INotificationAudienceRepository _audience;
    private readonly IServiceScopeFactory _scopes;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InvestmentAlertsJob> _logger;

    public InvestmentAlertsJob(
        INotificationAudienceRepository audience,
        IServiceScopeFactory scopes,
        IDateTimeProvider clock,
        ILogger<InvestmentAlertsJob> logger)
    {
        _audience = audience;
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Avisa al empleado cuando el valor de su portafolio cambia mas de 5% entre dos valuaciones diarias y "
        + "cuando se acerca el vencimiento de un plazo fijo o una caucion (a 7 y 1 dia), con la fecha y lo que "
        + "va a cobrar.";

    public string Schedule => "30 12 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = _clock.Today;
        var changed = 0;
        var maturing = 0;
        var failures = new List<string>();

        foreach (var employeeId in await _audience.ListEmployeesWithInvestmentsAsync(cancellationToken))
        {
            try
            {
                var (changedForEmployee, maturingForEmployee) = await AlertEmployeeAsync(employeeId, today, cancellationToken);
                changed += changedForEmployee;
                maturing += maturingForEmployee;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogEmployeeFailed(_logger, exception, employeeId);
                failures.Add($"Empleado {employeeId}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{changed} aviso(s) de variacion del portafolio, {maturing} de vencimiento, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    internal static IReadOnlyList<NotificationRequest> EvaluateValueChange(
        Guid employeeId,
        IReadOnlyCollection<Investment> investments,
        DateOnly today)
    {
        var requests = new List<NotificationRequest>();

        foreach (var byCurrency in investments.GroupBy(investment => investment.InvestedAmount.Currency))
        {
            var latest = byCurrency.Max(investment => investment.ValuedOn);

            if (latest < today.AddDays(-MaxValuationAgeDays))
            {
                continue;
            }

            var previous = latest.AddDays(-1);

            // Lo comprado despues de la fecha anterior no es una variacion, es capital nuevo.
            var comparable = byCurrency.Where(investment => investment.PurchasedOn <= previous).ToList();

            if (comparable.Count == 0)
            {
                continue;
            }

            var before = comparable.Sum(investment => investment.ValueOn(previous).Amount);
            var after = comparable.Sum(investment => investment.ValueOn(latest).Amount);

            if (before == 0m)
            {
                continue;
            }

            var percentage = (after - before) / before * 100m;

            if (Math.Abs(percentage) < ChangeThresholdPercent)
            {
                continue;
            }

            requests.Add(ValueChanged(employeeId, byCurrency.Key, before, after, percentage, previous, latest));
        }

        return requests;
    }

    internal static IReadOnlyList<NotificationRequest> EvaluateMaturity(
        Guid employeeId,
        IReadOnlyCollection<Investment> investments,
        DateOnly today)
    {
        var requests = new List<NotificationRequest>();

        foreach (var investment in investments)
        {
            if (investment.MaturesOn is not { } maturesOn || investment.ValueAtMaturity is not { } amount)
            {
                continue;
            }

            var daysRemaining = maturesOn.DayNumber - today.DayNumber;

            if (daysRemaining < 0)
            {
                continue;
            }

            var milestone = MaturityMilestones.FirstOrDefault(days => daysRemaining <= days);

            if (milestone > 0)
            {
                requests.Add(Maturing(employeeId, investment, maturesOn, amount, daysRemaining, milestone));
            }
        }

        return requests;
    }

    private async Task<(int Changed, int Maturing)> AlertEmployeeAsync(
        Guid employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var investments = scope.ServiceProvider.GetRequiredService<IInvestmentRepository>();
        var notifier = scope.ServiceProvider.GetRequiredService<Notifier>();

        var portfolio = await investments.ListByEmployeeAsync(employeeId, cancellationToken);
        var changed = 0;
        var maturing = 0;

        foreach (var request in EvaluateValueChange(employeeId, portfolio, today))
        {
            if (await notifier.NotifyAsync(request, cancellationToken) is not null)
            {
                changed++;
            }
        }

        foreach (var request in EvaluateMaturity(employeeId, portfolio, today))
        {
            if (await notifier.NotifyAsync(request, cancellationToken) is not null)
            {
                maturing++;
            }
        }

        return (changed, maturing);
    }

    private static NotificationRequest ValueChanged(
        Guid employeeId,
        Currency currency,
        decimal before,
        decimal after,
        decimal percentage,
        DateOnly previous,
        DateOnly latest)
    {
        var direction = percentage > 0m ? "subió" : "bajó";

        return new NotificationRequest(
            NotificationRecipient.Employee(employeeId),
            NotificationType.InvestmentValueChanged,
            ValueChangedTitle,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Tu portafolio en {currency} {direction} {Math.Abs(percentage).ToString("0.##", ArgentineNumbers)}% "
                + $"entre el {previous:dd/MM} y el {latest:dd/MM}: de {Format(before, currency)} a {Format(after, currency)}."),
            string.Create(CultureInfo.InvariantCulture, $"investment-value:{currency}:{latest:yyyy-MM-dd}"),
            ValueChangedDeduplicationWindow);
    }

    private static NotificationRequest Maturing(
        Guid employeeId,
        Investment investment,
        DateOnly maturesOn,
        Money amount,
        int daysRemaining,
        int milestone)
    {
        var timeLeft = daysRemaining switch
        {
            0 => "vence hoy",
            1 => "vence mañana",
            _ => string.Create(CultureInfo.InvariantCulture, $"vence en {daysRemaining} días"),
        };

        return new NotificationRequest(
            NotificationRecipient.Employee(employeeId),
            NotificationType.InvestmentMaturing,
            MaturingTitle,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Tu {investment.AssetName} {timeLeft} ({maturesOn:dd/MM/yyyy}). "
                + $"Vas a cobrar {Format(amount.Amount, amount.Currency)}."),
            string.Create(CultureInfo.InvariantCulture, $"investment-maturity:{investment.Id}:{maturesOn:yyyy-MM-dd}:{milestone}"),
            MaturityDeduplicationWindow);
    }

    private static string Format(decimal amount, Currency currency) =>
        $"{currency} {amount.ToString("#,0.##", ArgentineNumbers)}";

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la evaluacion de las inversiones del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
