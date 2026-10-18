namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Features.Notifications.Types.ListCompanyNotificationTypes;
using FinGrow.Application.UnitTests.Fakes;

public class ListCompanyNotificationTypesHandlerTests
{
    [Fact]
    public async Task Unconfigured_company_alert_types_are_disabled_by_default()
    {
        var result = await new ListCompanyNotificationTypesHandler(new FakeCompanyNotificationSettingRepository())
            .Handle(new ListCompanyNotificationTypesQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.Value.ShouldAllBe(type => !type.IsEnabled);
    }
}
