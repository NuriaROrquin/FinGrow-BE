namespace FinGrow.Application.UnitTests.Features.Integrations;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.GetIntegration;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class GetIntegrationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Guid _employeeId = Guid.CreateVersion7();

    [Fact]
    public async Task A_linked_employee_sees_their_account_and_when_it_was_linked()
    {
        _currentUser.UserId = _employeeId;
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(_employeeId, IntegrationProvider.WhatsApp, "+5491161972627", Now));

        var result = await Handle(IntegrationProvider.WhatsApp);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Linked.ShouldBeTrue();
        result.Value.ExternalAccountId.ShouldBe("+5491161972627");
        result.Value.LinkedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Each_provider_is_reported_separately()
    {
        _currentUser.UserId = _employeeId;
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(_employeeId, IntegrationProvider.Telegram, "123456789", Now));

        (await Handle(IntegrationProvider.Telegram)).Value.Linked.ShouldBeTrue();
        (await Handle(IntegrationProvider.WhatsApp)).Value.ShouldBe(IntegrationResponse.NotLinked);
    }

    [Fact]
    public async Task An_employee_without_integration_is_reported_as_not_linked()
    {
        _currentUser.UserId = _employeeId;

        var result = await Handle(IntegrationProvider.WhatsApp);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(IntegrationResponse.NotLinked);
    }

    [Fact]
    public async Task Another_employees_integration_does_not_count_as_linked()
    {
        _currentUser.UserId = _employeeId;
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.WhatsApp, "+5491100000000", Now));

        var result = await Handle(IntegrationProvider.WhatsApp);

        result.Value.Linked.ShouldBeFalse();
    }

    [Fact]
    public async Task An_anonymous_request_is_forbidden()
    {
        var result = await Handle(IntegrationProvider.WhatsApp);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private Task<Result<IntegrationResponse>> Handle(IntegrationProvider provider) =>
        new GetIntegrationHandler(_currentUser, _integrations)
            .Handle(new GetIntegrationQuery(provider), CancellationToken.None);
}
