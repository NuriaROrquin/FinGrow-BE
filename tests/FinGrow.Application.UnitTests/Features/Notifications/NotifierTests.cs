namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class NotifierTests
{
    private const long ChatId = 987654321;
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Window = TimeSpan.FromHours(12);

    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeNotificationChannelSettingRepository _channelSettings = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeTelegramBotClient _bot = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task A_triggered_alert_is_stored_unread_for_its_recipient()
    {
        var notification = await Notify(Request());

        notification.ShouldNotBeNull();
        var stored = _notifications.Notifications.ShouldHaveSingleItem();
        stored.ShouldBeSameAs(notification);
        stored.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        stored.Type.ShouldBe(NotificationType.TransactionsToReview);
        stored.Title.ShouldBe("Movimientos para revisar");
        stored.CreatedAt.ShouldBe(Now);
        stored.IsRead.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task With_telegram_linked_and_enabled_the_alert_also_goes_to_the_chat()
    {
        LinkTelegram();

        await Notify(Request());

        var sent = _bot.Sent.ShouldHaveSingleItem();
        sent.ChatId.ShouldBe(ChatId);
        sent.Text.ShouldBe("Movimientos para revisar\n\nMercado Pago trajo movimientos nuevos.");
    }

    [Fact]
    public async Task When_the_employee_turned_telegram_off_the_alert_stays_only_in_the_app()
    {
        LinkTelegram();
        _channelSettings.Add(NotificationChannelSetting.Create(EmployeeId, NotificationChannel.Telegram, isEnabled: false, Now));

        await Notify(Request());

        _notifications.Notifications.ShouldHaveSingleItem();
        _bot.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_channel_turned_back_on_delivers_again()
    {
        LinkTelegram();
        var setting = NotificationChannelSetting.Create(EmployeeId, NotificationChannel.Telegram, isEnabled: false, Now);
        setting.Change(isEnabled: true, Now);
        _channelSettings.Add(setting);

        await Notify(Request());

        _bot.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Without_telegram_linked_the_alert_stays_only_in_the_app()
    {
        await Notify(Request());

        _notifications.Notifications.ShouldHaveSingleItem();
        _bot.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_company_alert_is_never_sent_to_an_employee_chat()
    {
        LinkTelegram();

        await Notify(Request() with { Recipient = NotificationRecipient.Company(EmployeeId) });

        _notifications.Notifications.ShouldHaveSingleItem().RecipientType.ShouldBe(NotificationRecipientType.Company);
        _bot.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task If_telegram_does_not_answer_the_alert_is_kept_in_the_app_anyway()
    {
        LinkTelegram();
        _bot.Unreachable = true;

        var notification = await Notify(Request());

        notification.ShouldNotBeNull();
        _notifications.Notifications.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task The_same_condition_inside_the_window_is_not_notified_twice()
    {
        LinkTelegram();
        await Notify(Request());
        _clock.UtcNow = Now.AddHours(11);

        var repeated = await Notify(Request());

        repeated.ShouldBeNull();
        _notifications.Notifications.ShouldHaveSingleItem();
        _bot.Sent.ShouldHaveSingleItem();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Once_the_window_is_over_the_same_condition_is_notified_again()
    {
        await Notify(Request());
        _clock.UtcNow = Now.Add(Window).AddMinutes(1);

        var again = await Notify(Request());

        again.ShouldNotBeNull();
        _notifications.Notifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Another_condition_or_another_recipient_is_not_deduplicated()
    {
        await Notify(Request());

        var otherCondition = await Notify(Request() with { DeduplicationKey = "transactions-to-review:Gmail" });
        var otherEmployee = await Notify(Request() with { Recipient = NotificationRecipient.Employee(Guid.CreateVersion7()) });

        otherCondition.ShouldNotBeNull();
        otherEmployee.ShouldNotBeNull();
        _notifications.Notifications.Count.ShouldBe(3);
    }

    [Fact]
    public async Task The_condition_is_compared_without_surrounding_spaces()
    {
        await Notify(Request());

        var repeated = await Notify(Request() with { DeduplicationKey = "  transactions-to-review:MercadoPago  " });

        repeated.ShouldBeNull();
    }

    [Fact]
    public async Task A_window_of_zero_does_not_deduplicate_alerts_from_another_moment()
    {
        await Notify(Request() with { DeduplicationWindow = TimeSpan.Zero });
        _clock.UtcNow = Now.AddSeconds(1);

        var again = await Notify(Request() with { DeduplicationWindow = TimeSpan.Zero });

        again.ShouldNotBeNull();
    }

    private static NotificationRequest Request() => new(
        NotificationRecipient.Employee(EmployeeId),
        NotificationType.TransactionsToReview,
        "Movimientos para revisar",
        "Mercado Pago trajo movimientos nuevos.",
        "transactions-to-review:MercadoPago",
        Window);

    private void LinkTelegram() =>
        _integrations.Add(EmployeeIntegration.Create(
            EmployeeId, IntegrationProvider.Telegram, ChatId.ToString(System.Globalization.CultureInfo.InvariantCulture), Now.AddDays(-3)));

    private Task<Notification?> Notify(NotificationRequest request) =>
        new Notifier(
            _notifications,
            _channelSettings,
            new INotificationSender[] { new TelegramNotificationSender(_integrations, _bot) },
            _unitOfWork,
            _clock,
            NullLogger<Notifier>.Instance).NotifyAsync(request);
}
