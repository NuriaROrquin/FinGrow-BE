namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Features.Notifications.Types.ListCompanyNotificationTypes;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;

public class ListCompanyNotificationTypesHandlerTests
{
    [Fact]
    public async Task Unconfigured_company_alert_types_are_disabled_by_default()
    {
        var result = await new ListCompanyNotificationTypesHandler(new FakeCompanyNotificationSettingRepository())
            .Handle(new ListCompanyNotificationTypesQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.Value.ShouldAllBe(type => !type.IsEnabled);
    }

    [Fact]
    public async Task The_monthly_report_is_listed_as_a_monthly_company_preference_without_threshold()
    {
        var result = await new ListCompanyNotificationTypesHandler(new FakeCompanyNotificationSettingRepository())
            .Handle(new ListCompanyNotificationTypesQuery(Guid.CreateVersion7()), CancellationToken.None);

        var monthlyReport = result.Value.Single(type => type.Type == NotificationType.MonthlyReport);
        monthlyReport.IsEnabled.ShouldBeFalse();
        monthlyReport.Cadence.ShouldBe(NotificationCadence.Monthly);
        monthlyReport.Threshold.ShouldBeNull();
    }
}
