namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;

public class CompanyNotificationSettingTests
{
    private static readonly Guid CompanyId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Company_alerts_can_only_have_a_monthly_cadence()
    {
        var setting = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.EmployeeAdded, isEnabled: true, NotificationCadence.Monthly, Now);

        setting.Cadence.ShouldBe(NotificationCadence.Monthly);
        setting.IsDue(Now).ShouldBeTrue();
        setting.MarkEvaluated(Now);
        setting.IsDue(Now.AddDays(29)).ShouldBeFalse();
        setting.IsDue(Now.AddMonths(1)).ShouldBeTrue();
        Should.Throw<DomainException>(() => CompanyNotificationSetting.Create(
            CompanyId, NotificationType.EmployeeAdded, isEnabled: true, NotificationCadence.Daily, Now));
    }

    [Fact]
    public void Financial_wellbeing_can_only_be_monthly()
    {
        Should.Throw<DomainException>(() => CompanyNotificationSetting.Create(
            CompanyId, NotificationType.FinancialWellbeingLow, isEnabled: true, NotificationCadence.Weekly, Now));
    }

    [Fact]
    public void Monthly_alerts_accept_a_custom_threshold_between_one_and_one_hundred()
    {
        var setting = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.FinancialWellbeingLow, isEnabled: true, NotificationCadence.Monthly, Now, threshold: 72.5m);

        setting.Threshold.ShouldBe(72.5m);
    }

    [Fact]
    public void Monthly_alerts_reject_an_invalid_threshold()
    {
        Should.Throw<DomainException>(() => CompanyNotificationSetting.Create(
            CompanyId, NotificationType.ParticipationLow, isEnabled: true, NotificationCadence.Monthly, Now, threshold: 0m));
    }

    [Fact]
    public void Employee_alert_types_cannot_be_configured_for_a_company()
    {
        Should.Throw<DomainException>(() => CompanyNotificationSetting.Create(
            CompanyId, NotificationType.GoalStalled, isEnabled: true, NotificationCadence.Monthly, Now));
    }

    [Fact]
    public void The_monthly_report_is_a_company_preference_but_not_an_alert()
    {
        var setting = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.MonthlyReport, isEnabled: true, NotificationCadence.Monthly, Now);

        setting.Threshold.ShouldBeNull();
        CompanyNotificationSetting.IsCompanyType(NotificationType.MonthlyReport).ShouldBeTrue();
        CompanyNotificationSetting.IsAlertType(NotificationType.MonthlyReport).ShouldBeFalse();
        CompanyNotificationSetting.IsAlertType(NotificationType.ParticipationLow).ShouldBeTrue();
        Should.Throw<DomainException>(() => CompanyNotificationSetting.Create(
            CompanyId, NotificationType.MonthlyReport, isEnabled: true, NotificationCadence.Monthly, Now, threshold: 50m));
    }

    [Fact]
    public void A_setting_created_enabled_remembers_since_when()
    {
        var enabled = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.MonthlyReport, isEnabled: true, NotificationCadence.Monthly, Now);
        var disabled = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.MonthlyReport, isEnabled: false, NotificationCadence.Monthly, Now);

        enabled.EnabledAt.ShouldBe(Now);
        disabled.EnabledAt.ShouldBeNull();
    }

    [Fact]
    public void Saving_an_enabled_setting_again_keeps_the_moment_it_was_turned_on()
    {
        var setting = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.MonthlyReport, isEnabled: true, NotificationCadence.Monthly, Now);

        setting.Change(isEnabled: true, NotificationCadence.Monthly, Now.AddDays(10));

        setting.EnabledAt.ShouldBe(Now);
        setting.UpdatedAt.ShouldBe(Now.AddDays(10));
    }

    [Fact]
    public void Turning_a_setting_off_forgets_when_it_was_on_and_turning_it_on_again_starts_over()
    {
        var setting = CompanyNotificationSetting.Create(
            CompanyId, NotificationType.MonthlyReport, isEnabled: true, NotificationCadence.Monthly, Now);

        setting.Change(isEnabled: false, NotificationCadence.Monthly, Now.AddDays(1));
        setting.EnabledAt.ShouldBeNull();

        setting.Change(isEnabled: true, NotificationCadence.Monthly, Now.AddDays(2));
        setting.EnabledAt.ShouldBe(Now.AddDays(2));
    }
}
