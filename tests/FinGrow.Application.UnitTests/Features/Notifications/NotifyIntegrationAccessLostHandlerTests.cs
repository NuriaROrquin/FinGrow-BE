namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.IntegrationAccessLost;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class NotifyIntegrationAccessLostHandlerTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task Losing_gmail_access_tells_the_employee_to_reauthorize()
    {
        await Handle(IntegrationProvider.Gmail);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Type.ShouldBe(NotificationType.IntegrationAccessLost);
        notification.Title.ShouldBe("Perdimos el acceso a Gmail");
        notification.Body.ShouldContain("Volve a autorizarlo");
        notification.DeduplicationKey.ShouldBe("integration-access-lost:Gmail");
    }

    [Fact]
    public async Task Each_provider_gets_its_own_alert()
    {
        await Handle(IntegrationProvider.Gmail);
        await Handle(IntegrationProvider.Gmail);
        await Handle(IntegrationProvider.MercadoPago);

        _notifications.Notifications.Count.ShouldBe(2);
        _notifications.Notifications[0].Title.ShouldBe("Perdimos el acceso a Gmail");
        _notifications.Notifications[1].Title.ShouldBe("Perdimos el acceso a Mercado Pago");
    }

    private Task Handle(IntegrationProvider provider)
    {
        var notifier = new Notifier(
            _notifications,
            new FakeNotificationChannelSettingRepository(),
            Array.Empty<INotificationSender>(),
            new FakeUnitOfWork(),
            _clock,
            NullLogger<Notifier>.Instance);

        return new NotifyIntegrationAccessLostHandler(notifier).Handle(
            new DomainEventEnvelope<IntegrationAccessLost>(
                new IntegrationAccessLost(Guid.CreateVersion7(), EmployeeId, provider, _clock.UtcNow)),
            CancellationToken.None);
    }
}
