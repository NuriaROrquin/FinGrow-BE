namespace FinGrow.Application.Features.Achievements.Unlock;

using FinGrow.Application.Events;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class UnlockInstantAchievementsHandler(
    AchievementGranter granter,
    ILogger<UnlockInstantAchievementsHandler> logger) :
    INotificationHandler<DomainEventEnvelope<TransactionConfirmed>>,
    INotificationHandler<DomainEventEnvelope<GoalCreated>>,
    INotificationHandler<DomainEventEnvelope<GoalAchieved>>,
    INotificationHandler<DomainEventEnvelope<BudgetLimitSet>>,
    INotificationHandler<DomainEventEnvelope<IntegrationLinked>>,
    INotificationHandler<DomainEventEnvelope<InvestmentAdded>>,
    INotificationHandler<DomainEventEnvelope<LessonCompleted>>
{
    private static readonly AchievementCode[] FromTransaction = { AchievementCode.FirstTransaction, AchievementCode.ReceiptScanned };
    private static readonly AchievementCode[] FromGoalCreated = { AchievementCode.FirstGoal };
    private static readonly AchievementCode[] FromGoalAchieved = { AchievementCode.GoalAchieved, AchievementCode.ThreeGoalsAchieved };
    private static readonly AchievementCode[] FromBudgetLimit = { AchievementCode.FirstBudget };
    private static readonly AchievementCode[] FromIntegration = { AchievementCode.ChannelLinked };
    private static readonly AchievementCode[] FromInvestment = { AchievementCode.FirstInvestment };
    private static readonly AchievementCode[] FromLesson = { AchievementCode.FirstCourse };

    public Task Handle(DomainEventEnvelope<TransactionConfirmed> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromTransaction, cancellationToken);
    }

    public Task Handle(DomainEventEnvelope<GoalCreated> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromGoalCreated, cancellationToken);
    }

    public Task Handle(DomainEventEnvelope<GoalAchieved> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromGoalAchieved, cancellationToken);
    }

    public Task Handle(DomainEventEnvelope<BudgetLimitSet> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromBudgetLimit, cancellationToken);
    }

    public Task Handle(DomainEventEnvelope<IntegrationLinked> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromIntegration, cancellationToken);
    }

    public Task Handle(DomainEventEnvelope<InvestmentAdded> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromInvestment, cancellationToken);
    }

    public Task Handle(DomainEventEnvelope<LessonCompleted> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return UnlockAsync(notification.DomainEvent.EmployeeId, FromLesson, cancellationToken);
    }

    private async Task UnlockAsync(Guid employeeId, AchievementCode[] codes, CancellationToken cancellationToken)
    {
        try
        {
            await granter.GrantInstantAsync(employeeId, codes, AchievementGrantMode.Live, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogUnlockFailed(logger, exception, employeeId, string.Join(", ", codes));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudieron otorgar los logros {Codes} al empleado {EmployeeId}; los recupera el trabajo achievements-evaluation.")]
    private static partial void LogUnlockFailed(ILogger logger, Exception exception, Guid employeeId, string codes);
}
