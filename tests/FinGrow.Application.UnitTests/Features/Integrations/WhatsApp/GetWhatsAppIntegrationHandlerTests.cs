namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.WhatsApp.GetWhatsAppIntegration;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class GetWhatsAppIntegrationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Guid _employeeId = Guid.CreateVersion7();

    [Fact]
    public async Task A_linked_employee_sees_their_number_and_when_it_was_linked()
    {
        _currentUser.UserId = _employeeId;
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(_employeeId, IntegrationProvider.WhatsApp, "+5491161972627", Now));

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Linked.ShouldBeTrue();
        result.Value.PhoneNumber.ShouldBe("+5491161972627");
        result.Value.LinkedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task An_employee_without_integration_is_reported_as_not_linked()
    {
        _currentUser.UserId = _employeeId;

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(WhatsAppIntegrationResponse.NotLinked);
    }

    [Fact]
    public async Task Another_employees_integration_does_not_count_as_linked()
    {
        _currentUser.UserId = _employeeId;
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.WhatsApp, "+5491100000000", Now));

        var result = await Handle();

        result.Value.Linked.ShouldBeFalse();
    }

    [Fact]
    public async Task An_anonymous_request_is_forbidden()
    {
        var result = await Handle();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private Task<Result<WhatsAppIntegrationResponse>> Handle() =>
        new GetWhatsAppIntegrationHandler(_currentUser, _integrations)
            .Handle(new GetWhatsAppIntegrationQuery(), CancellationToken.None);
}
