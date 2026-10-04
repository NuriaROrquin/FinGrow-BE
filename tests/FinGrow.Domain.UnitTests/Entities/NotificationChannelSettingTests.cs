namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class NotificationChannelSettingTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_external_channel_can_be_turned_off_and_on_again()
    {
        var setting = NotificationChannelSetting.Create(EmployeeId, NotificationChannel.Telegram, isEnabled: false, Now);

        setting.IsEnabled.ShouldBeFalse();

        setting.Change(isEnabled: true, Now.AddDays(1));

        setting.IsEnabled.ShouldBeTrue();
        setting.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void The_in_app_inbox_is_not_configurable()
    {
        NotificationChannel.InApp.IsConfigurable().ShouldBeFalse();
        NotificationChannel.Telegram.IsConfigurable().ShouldBeTrue();

        Should.Throw<DomainException>(() =>
            NotificationChannelSetting.Create(EmployeeId, NotificationChannel.InApp, isEnabled: false, Now));
    }

    [Fact]
    public void A_setting_always_belongs_to_an_employee()
    {
        Should.Throw<DomainException>(() =>
            NotificationChannelSetting.Create(Guid.Empty, NotificationChannel.Telegram, isEnabled: true, Now));
    }
}
