namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.BudgetThreshold;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class NotifyBudgetThresholdHandlerTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateOnly PeriodStart = new(2026, 10, 1);
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeBudgetSpendingReadRepository _spending = new();

    [Fact]
    public async Task Crossing_warning_threshold_raises_a_warning_alert()
    {
        AddBudget(1000m);
        _spending.Spent[ExpenseCategory.Alimentos] = 850m;

        await Handle(100m);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.BudgetWarning);
        notification.Title.ShouldContain("Alimentos");
    }

    [Fact]
    public async Task Crossing_limit_raises_a_distinct_exceeded_alert()
    {
        AddBudget(1000m);
        _spending.Spent[ExpenseCategory.Alimentos] = 1100m;

        await Handle(150m);

        _notifications.Notifications.ShouldHaveSingleItem().Type.ShouldBe(NotificationType.BudgetExceeded);
    }

    [Fact]
    public async Task The_same_threshold_alert_is_deduplicated_for_the_period()
    {
        AddBudget(1000m);
        _spending.Spent[ExpenseCategory.Alimentos] = 850m;

        await Handle(100m);
        await Handle(100m);

        _notifications.Notifications.ShouldHaveSingleItem();
    }

    private void AddBudget(decimal limitAmount)
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, PeriodStart, Now);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(limitAmount, Currency.ARS), Now);
        _budgets.Add(budget);
    }

    private Task Handle(decimal amount)
    {
        var notifier = new Notifier(
            _notifications,
            new FakeNotificationChannelSettingRepository(),
            Array.Empty<INotificationSender>(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now),
            NullLogger<Notifier>.Instance);

        var handler = new NotifyBudgetThresholdHandler(_budgets, _spending, notifier);

        return handler.Handle(
            new DomainEventEnvelope<TransactionConfirmed>(
                new TransactionConfirmed(
                    Guid.CreateVersion7(),
                    EmployeeId,
                    ExpenseCategory.Alimentos,
                    amount,
                    Currency.ARS,
                    new DateOnly(2026, 10, 9),
                    Now)),
            CancellationToken.None);
    }
}
