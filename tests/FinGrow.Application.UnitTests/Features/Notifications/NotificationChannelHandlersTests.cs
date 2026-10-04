namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Notifications.Channels.ChangeNotificationChannel;
using FinGrow.Application.Features.Notifications.Channels.ListNotificationChannels;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class NotificationChannelHandlersTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationChannelSettingRepository _channelSettings = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task Without_choices_every_channel_is_enabled_and_the_app_is_fixed()
    {
        var channels = (await List()).Value;

        channels.Count.ShouldBe(2);
        var inApp = channels.Single(channel => channel.Channel == NotificationChannel.InApp);
        inApp.IsEnabled.ShouldBeTrue();
        inApp.IsConfigurable.ShouldBeFalse();
        inApp.IsConnected.ShouldBeTrue();
        var telegram = channels.Single(channel => channel.Channel == NotificationChannel.Telegram);
        telegram.IsEnabled.ShouldBeTrue();
        telegram.IsConfigurable.ShouldBeTrue();
        telegram.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public async Task Telegram_shows_as_connected_once_the_chat_is_linked()
    {
        _integrations.Add(EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Telegram, "987654321", Now));

        var channels = (await List()).Value;

        channels.Single(channel => channel.Channel == NotificationChannel.Telegram).IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task Turning_a_channel_off_stores_the_choice_and_the_list_reflects_it()
    {
        var result = await Change(NotificationChannel.Telegram, isEnabled: false);

        result.Value.IsEnabled.ShouldBeFalse();
        var stored = _channelSettings.Settings.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(EmployeeId);
        stored.IsEnabled.ShouldBeFalse();
        stored.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
        (await List()).Value.Single(channel => channel.Channel == NotificationChannel.Telegram).IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Changing_it_again_updates_the_same_choice()
    {
        await Change(NotificationChannel.Telegram, isEnabled: false);
        _clock.UtcNow = Now.AddDays(1);

        var result = await Change(NotificationChannel.Telegram, isEnabled: true);

        result.Value.IsEnabled.ShouldBeTrue();
        var stored = _channelSettings.Settings.ShouldHaveSingleItem();
        stored.IsEnabled.ShouldBeTrue();
        stored.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void The_app_inbox_cannot_be_turned_off()
    {
        var validator = new ChangeNotificationChannelCommandValidator();

        validator.Validate(new ChangeNotificationChannelCommand(EmployeeId, NotificationChannel.InApp, IsEnabled: false))
            .IsValid.ShouldBeFalse();
        validator.Validate(new ChangeNotificationChannelCommand(EmployeeId, (NotificationChannel)99, IsEnabled: false))
            .IsValid.ShouldBeFalse();
        validator.Validate(new ChangeNotificationChannelCommand(EmployeeId, NotificationChannel.Telegram, IsEnabled: false))
            .IsValid.ShouldBeTrue();
    }

    private Task<Result<IReadOnlyList<NotificationChannelResponse>>> List() =>
        new ListNotificationChannelsHandler(_channelSettings, _integrations)
            .Handle(new ListNotificationChannelsQuery(EmployeeId), CancellationToken.None);

    private Task<Result<NotificationChannelResponse>> Change(
        NotificationChannel channel,
        bool isEnabled) =>
        new ChangeNotificationChannelHandler(_channelSettings, _integrations, _unitOfWork, _clock)
            .Handle(new ChangeNotificationChannelCommand(EmployeeId, channel, isEnabled), CancellationToken.None);
}
