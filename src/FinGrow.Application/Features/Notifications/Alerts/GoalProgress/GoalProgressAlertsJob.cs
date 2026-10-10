namespace FinGrow.Application.Features.Notifications.Alerts.GoalProgress;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class GoalProgressAlertsJob : IScheduledJob
{
    public const string JobName = "goal-progress-alerts";

    internal const string StalledTitle = "Tu meta está quieta";
    internal const string DeadlineTitle = "Tu meta está por vencer";

    internal const int StalledAfterDays = 30;

    internal static readonly int[] DeadlineMilestones = { 1, 7, 30 };

    internal static readonly TimeSpan StalledDeduplicationWindow = TimeSpan.FromDays(StalledAfterDays - 1);

    internal static readonly TimeSpan DeadlineDeduplicationWindow = TimeSpan.FromDays(366 * 5);

    private static readonly NumberFormatInfo ArgentineNumbers = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
    };

    private readonly IGoalRepository _goals;
    private readonly IServiceScopeFactory _scopes;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<GoalProgressAlertsJob> _logger;

    public GoalProgressAlertsJob(
        IGoalRepository goals,
        IServiceScopeFactory scopes,
        IDateTimeProvider clock,
        ILogger<GoalProgressAlertsJob> logger)
    {
        _goals = goals;
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        $"Avisa al empleado cuando una meta activa lleva {StalledAfterDays} dias sin aportes y cuando se acerca su "
        + "fecha limite con saldo pendiente (a 30, 7 y 1 dia), indicando cuanto falta y cuantos dias quedan.";

    public string Schedule => "0 12 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = _clock.Today;
        var stalled = 0;
        var approaching = 0;
        var failures = new List<string>();

        foreach (var employeeId in await _goals.ListEmployeesWithActiveGoalsAsync(cancellationToken))
        {
            try
            {
                var (stalledForEmployee, approachingForEmployee) = await AlertEmployeeAsync(employeeId, today, cancellationToken);
                stalled += stalledForEmployee;
                approaching += approachingForEmployee;
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
            $"{stalled} aviso(s) de meta sin aportes, {approaching} de fecha limite cercana, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    private async Task<(int Stalled, int Approaching)> AlertEmployeeAsync(
        Guid employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var goals = scope.ServiceProvider.GetRequiredService<IGoalRepository>();
        var notifier = scope.ServiceProvider.GetRequiredService<Notifier>();

        var stalled = 0;
        var approaching = 0;

        foreach (var goal in await goals.ListByEmployeeAsync(employeeId, cancellationToken))
        {
            if (Evaluate(goal, today) is not { } request)
            {
                continue;
            }

            if (await notifier.NotifyAsync(request, cancellationToken) is null)
            {
                continue;
            }

            if (request.Type == NotificationType.GoalStalled)
            {
                stalled++;
            }
            else
            {
                approaching++;
            }
        }

        return (stalled, approaching);
    }

    internal static NotificationRequest? Evaluate(Goal goal, DateOnly today)
    {
        if (goal.Status != GoalStatus.Active)
        {
            return null;
        }

        var daysRemaining = goal.DaysRemaining(today);

        if (daysRemaining < 0)
        {
            return null;
        }

        var milestone = DeadlineMilestones.FirstOrDefault(days => daysRemaining <= days);

        if (milestone > 0)
        {
            return DeadlineApproaching(goal, daysRemaining, milestone);
        }

        var idleDays = today.DayNumber - LastActivityOn(goal).DayNumber;

        return idleDays >= StalledAfterDays ? Stalled(goal, idleDays) : null;
    }

    private static NotificationRequest DeadlineApproaching(Goal goal, int daysRemaining, int milestone)
    {
        var timeLeft = daysRemaining switch
        {
            0 => "vence hoy",
            1 => "queda 1 día",
            _ => string.Create(CultureInfo.InvariantCulture, $"quedan {daysRemaining} días"),
        };

        var deadline = daysRemaining == 0
            ? string.Empty
            : string.Create(CultureInfo.InvariantCulture, $" (vence el {goal.Deadline:dd/MM/yyyy})");

        return new NotificationRequest(
            NotificationRecipient.Employee(goal.EmployeeId),
            NotificationType.GoalDeadlineApproaching,
            DeadlineTitle,
            $"Te faltan {Format(goal.RemainingAmount)} para completar \"{goal.Name}\" y {timeLeft}{deadline}.",
            string.Create(CultureInfo.InvariantCulture, $"goal-deadline:{goal.Id}:{goal.Deadline:yyyy-MM-dd}:{milestone}"),
            DeadlineDeduplicationWindow);
    }

    private static NotificationRequest Stalled(Goal goal, int idleDays) =>
        new(
            NotificationRecipient.Employee(goal.EmployeeId),
            NotificationType.GoalStalled,
            StalledTitle,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Hace {idleDays} días que no sumás nada a \"{goal.Name}\". Llevás el {goal.ProgressPercentage:0.#}% "
                + $"y te faltan {Format(goal.RemainingAmount)}: un aporte chico ya te acerca."),
            $"goal-stalled:{goal.Id}",
            StalledDeduplicationWindow);

    private static DateOnly LastActivityOn(Goal goal)
    {
        var createdOn = ArgentinaTime.DateOf(goal.CreatedAt);
        var lastContribution = goal.Contributions.Count == 0
            ? createdOn
            : goal.Contributions.Max(contribution => contribution.ContributedOn);

        return lastContribution > createdOn ? lastContribution : createdOn;
    }

    private static string Format(Money money) =>
        $"{money.Currency} {money.Amount.ToString("#,0.##", ArgentineNumbers)}";

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la evaluacion de las metas del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
