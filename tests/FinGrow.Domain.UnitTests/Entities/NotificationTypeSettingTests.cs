namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class NotificationTypeSettingTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_alert_type_can_be_turned_off_and_on_again()
    {
        var setting = NotificationTypeSetting.Create(EmployeeId, NotificationType.BudgetWarning, isEnabled: false, Now);

        setting.IsEnabled.ShouldBeFalse();

        setting.Change(isEnabled: true, Now.AddDays(1));

        setting.IsEnabled.ShouldBeTrue();
        setting.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public void A_setting_always_belongs_to_an_employee()
    {
        Should.Throw<DomainException>(() =>
            NotificationTypeSetting.Create(Guid.Empty, NotificationType.BudgetWarning, isEnabled: true, Now));
    }

    [Fact]
    public void An_alert_type_that_does_not_exist_is_rejected()
    {
        Should.Throw<DomainException>(() =>
            NotificationTypeSetting.Create(EmployeeId, (NotificationType)99, isEnabled: true, Now));
    }
}
