namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.TransactionsToReview;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class NotifyTransactionsToReviewHandlerTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task Movements_brought_by_mercado_pago_raise_a_review_alert()
    {
        await Handle(TransactionSource.MercadoPago);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Type.ShouldBe(NotificationType.TransactionsToReview);
        notification.Title.ShouldBe(NotifyTransactionsToReviewHandler.Title);
        notification.Body.ShouldContain("Mercado Pago");
        notification.DeduplicationKey.ShouldBe("transactions-to-review:MercadoPago");
    }

    [Fact]
    public async Task A_whole_sync_of_movements_ends_up_in_a_single_alert()
    {
        await Handle(TransactionSource.MercadoPago);
        await Handle(TransactionSource.MercadoPago);
        _clock.UtcNow = Now.AddHours(1);
        await Handle(TransactionSource.MercadoPago);

        _notifications.Notifications.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Gmail_is_an_origin_of_its_own()
    {
        await Handle(TransactionSource.MercadoPago);
        await Handle(TransactionSource.Gmail);

        _notifications.Notifications.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(TransactionSource.Manual)]
    [InlineData(TransactionSource.ReceiptScan)]
    [InlineData(TransactionSource.Telegram)]
    [InlineData(TransactionSource.WhatsApp)]
    public async Task What_the_employee_loads_personally_does_not_need_an_alert(TransactionSource source)
    {
        await Handle(source);

        _notifications.Notifications.ShouldBeEmpty();
    }

    private Task Handle(TransactionSource source)
    {
        var notifier = new Notifier(
            _notifications,
            new FakeNotificationChannelSettingRepository(),
            Array.Empty<INotificationSender>(),
            new FakeUnitOfWork(),
            _clock,
            NullLogger<Notifier>.Instance);

        return new NotifyTransactionsToReviewHandler(notifier).Handle(
            new DomainEventEnvelope<TransactionProposed>(
                new TransactionProposed(Guid.CreateVersion7(), EmployeeId, source, _clock.UtcNow)),
            CancellationToken.None);
    }
}
