namespace FinGrow.Application.Features.Notifications.Alerts.BudgetThreshold;

using FinGrow.Application.Events;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class NotifyBudgetThresholdHandler(
    IBudgetRepository budgetRepository,
    IBudgetSpendingReadRepository spendingReadRepository,
    Notifier notifier)
    : INotificationHandler<DomainEventEnvelope<TransactionConfirmed>>
{
    internal const string WarningTitle = "Te estas acercando al tope";
    internal const string ExceededTitle = "Superaste el tope";
    internal static readonly TimeSpan DeduplicationWindow = TimeSpan.FromDays(366);

    public async Task Handle(
        DomainEventEnvelope<TransactionConfirmed> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var confirmed = notification.DomainEvent;

        if (confirmed.ExpenseCategory is not { } category)
        {
            return;
        }

        var periodStart = new DateOnly(confirmed.OccurredOn.Year, confirmed.OccurredOn.Month, 1);
        var budget = await budgetRepository.FindForPeriodAsync(
            confirmed.EmployeeId,
            BudgetPeriod.Monthly,
            periodStart,
            cancellationToken);

        if (budget is null || budget.LimitFor(category) is not { } limit
            || limit.Currency != confirmed.Currency)
        {
            return;
        }

        var spentByCategory = await spendingReadRepository.GetSpentByCategoryAsync(budget, cancellationToken);
        var currentSpent = Money.From(spentByCategory.GetValueOrDefault(category), confirmed.Currency);
        var previousSpent = currentSpent.Amount >= confirmed.Amount
            ? Money.From(currentSpent.Amount - confirmed.Amount, confirmed.Currency)
            : Money.Zero(confirmed.Currency);
        var previousHealth = budget.Evaluate(category, previousSpent);
        var currentHealth = budget.Evaluate(category, currentSpent);

        if (previousHealth == BudgetHealth.OnTrack && currentHealth == BudgetHealth.Warning)
        {
            await NotifyAsync(
                confirmed,
                category,
                budget,
                NotificationType.BudgetWarning,
                WarningTitle,
                $"El consumo de {category} alcanzo el {currentSpent.PercentageOf(limit)}% del limite mensual.",
                cancellationToken);
        }
        else if (previousHealth != BudgetHealth.Exceeded && currentHealth == BudgetHealth.Exceeded)
        {
            await NotifyAsync(
                confirmed,
                category,
                budget,
                NotificationType.BudgetExceeded,
                ExceededTitle,
                $"El consumo de {category} supero el limite mensual de {limit}.",
                cancellationToken);
        }
    }

    private async Task NotifyAsync(
        TransactionConfirmed confirmed,
        ExpenseCategory category,
        Budget budget,
        NotificationType type,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Employee(confirmed.EmployeeId),
                type,
                $"{title} de {category}",
                body,
                $"budget:{budget.Id}:{category}:{type}:{budget.PeriodStart:yyyy-MM-dd}",
                DeduplicationWindow),
            cancellationToken);
    }
}
