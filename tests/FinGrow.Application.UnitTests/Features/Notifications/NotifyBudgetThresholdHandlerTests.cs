namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Events;
using FinGrow.Application.Features.Budgets.DeleteBudget;
using FinGrow.Application.Features.Budgets.RemoveCategoryLimit;
using FinGrow.Application.Features.Budgets.SetCategoryLimit;
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
    private const decimal Limit = 100000m;
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly September = new(2026, 9, 1);
    private static readonly DateOnly MidSeptember = new(2026, 9, 15);

    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeBudgetSpendingReadRepository _spending = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task Reaching_80_percent_of_the_limit_raises_a_warning()
    {
        var budget = BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Type.ShouldBe(NotificationType.BudgetWarning);
        notification.Title.ShouldBe($"{NotifyBudgetThresholdHandler.WarningTitle} de Alimentos");
        notification.Body.ShouldContain("Alimentos");
        notification.DeduplicationKey.ShouldBe(
            NotifyBudgetThresholdHandler.DeduplicationKey(budget, LimitOf(budget, ExpenseCategory.Alimentos), NotificationType.BudgetWarning));
    }

    [Fact]
    public async Task Going_over_the_limit_raises_an_exceeded_alert()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Alimentos, amount: 30000m, spentAfter: 105000m);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.BudgetExceeded);
        notification.Title.ShouldBe($"{NotifyBudgetThresholdHandler.ExceededTitle} de Alimentos");
    }

    [Fact]
    public async Task A_movement_that_keeps_the_category_on_track_does_not_alert()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 50000m);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_category_already_in_warning_is_not_warned_again()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);
        await Confirm(ExpenseCategory.Alimentos, amount: 5000m, spentAfter: 90000m);

        _notifications.Notifications.ShouldHaveSingleItem().Type.ShouldBe(NotificationType.BudgetWarning);
    }

    [Fact]
    public async Task The_same_threshold_is_notified_once_per_month()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);
        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        _notifications.Notifications.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_month_without_a_budget_does_not_alert()
    {
        await Confirm(ExpenseCategory.Alimentos, amount: 30000m, spentAfter: 105000m);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_category_without_a_limit_does_not_alert()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Salud, amount: 30000m, spentAfter: 105000m);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_movement_in_another_currency_does_not_alert()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(ExpenseCategory.Alimentos, amount: 30000m, spentAfter: 105000m, currency: Currency.USD);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_income_does_not_alert()
    {
        BudgetWith(ExpenseCategory.Alimentos);

        await Confirm(category: null, amount: 30000m, spentAfter: 105000m);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_deleted_budget_no_longer_raises_alerts()
    {
        BudgetWith(ExpenseCategory.Alimentos, ExpenseCategory.Transporte);
        var deleted = await new DeleteBudgetHandler(_budgets, new FakeUnitOfWork())
            .Handle(new DeleteBudgetCommand(EmployeeId, 2026, 9), CancellationToken.None);
        deleted.IsSuccess.ShouldBeTrue();

        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);
        await Confirm(ExpenseCategory.Transporte, amount: 30000m, spentAfter: 105000m);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_removed_category_no_longer_raises_alerts_but_the_others_still_do()
    {
        BudgetWith(ExpenseCategory.Alimentos, ExpenseCategory.Transporte);
        var removed = await RemoveLimit(ExpenseCategory.Transporte);
        removed.IsSuccess.ShouldBeTrue();

        await Confirm(ExpenseCategory.Transporte, amount: 30000m, spentAfter: 105000m);
        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Title.ShouldBe($"{NotifyBudgetThresholdHandler.WarningTitle} de Alimentos");
    }

    [Fact]
    public async Task A_category_removed_and_added_again_can_alert_again()
    {
        BudgetWith(ExpenseCategory.Alimentos, ExpenseCategory.Transporte);
        await Confirm(ExpenseCategory.Transporte, amount: 10000m, spentAfter: 85000m);

        await RemoveLimit(ExpenseCategory.Transporte);
        await SetLimit(ExpenseCategory.Transporte, Limit);
        await Confirm(ExpenseCategory.Transporte, amount: 10000m, spentAfter: 85000m);

        _notifications.Notifications.Count.ShouldBe(2);
        _notifications.Notifications.Select(notification => notification.DeduplicationKey).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task Editing_the_amount_of_a_limit_does_not_repeat_an_alert_already_sent()
    {
        BudgetWith(ExpenseCategory.Alimentos);
        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        await SetLimit(ExpenseCategory.Alimentos, 95000m);
        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        _notifications.Notifications.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_budget_deleted_and_created_again_can_alert_again()
    {
        BudgetWith(ExpenseCategory.Alimentos);
        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        await new DeleteBudgetHandler(_budgets, new FakeUnitOfWork())
            .Handle(new DeleteBudgetCommand(EmployeeId, 2026, 9), CancellationToken.None);
        BudgetWith(ExpenseCategory.Alimentos);
        await Confirm(ExpenseCategory.Alimentos, amount: 10000m, spentAfter: 85000m);

        _notifications.Notifications.Count.ShouldBe(2);
    }

    private static BudgetCategoryLimit LimitOf(Budget budget, ExpenseCategory category) =>
        budget.Limits.Single(limit => limit.Category == category);

    private Budget BudgetWith(params ExpenseCategory[] categories)
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, September, Now);

        foreach (var category in categories)
        {
            budget.SetLimit(category, Money.From(Limit, Currency.ARS), Now);
        }

        _budgets.Add(budget);

        return budget;
    }

    private Task<Result<BudgetResponse>> RemoveLimit(ExpenseCategory category) =>
        new RemoveBudgetCategoryLimitHandler(_budgets, _spending, new FakeUnitOfWork(), _clock)
            .Handle(new RemoveBudgetCategoryLimitCommand(EmployeeId, 2026, 9, category), CancellationToken.None);

    private Task<Result<BudgetResponse>> SetLimit(ExpenseCategory category, decimal amount) =>
        new SetBudgetCategoryLimitHandler(_budgets, _spending, new FakeUnitOfWork(), _clock)
            .Handle(new SetBudgetCategoryLimitCommand(EmployeeId, 2026, 9, category, amount), CancellationToken.None);

    private Task Confirm(
        ExpenseCategory? category,
        decimal amount,
        decimal spentAfter,
        Currency currency = Currency.ARS)
    {
        if (category is { } expenseCategory)
        {
            _spending.Spent[expenseCategory] = spentAfter;
        }

        var notifier = new Notifier(
            _notifications,
            new FakeNotificationChannelSettingRepository(),
            new FakeNotificationTypeSettingRepository(),
            new FakeCompanyNotificationSettingRepository(),
            Array.Empty<INotificationSender>(),
            new FakeUnitOfWork(),
            _clock,
            NullLogger<Notifier>.Instance);

        return new NotifyBudgetThresholdHandler(_budgets, _spending, notifier).Handle(
            new DomainEventEnvelope<TransactionConfirmed>(
                new TransactionConfirmed(
                    Guid.CreateVersion7(),
                    EmployeeId,
                    amount,
                    currency,
                    category,
                    MidSeptember,
                    _clock.UtcNow)),
            CancellationToken.None);
    }
}
