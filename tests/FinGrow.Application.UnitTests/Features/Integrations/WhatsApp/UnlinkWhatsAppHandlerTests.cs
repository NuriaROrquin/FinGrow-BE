namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.WhatsApp.UnlinkWhatsApp;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

public class UnlinkWhatsAppHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Guid _employeeId = Guid.CreateVersion7();

    [Fact]
    public async Task A_linked_employee_removes_their_integration()
    {
        _currentUser.UserId = _employeeId;
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(_employeeId, IntegrationProvider.WhatsApp, "+5491161972627", Now));

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        _integrations.Integrations.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Only_the_current_employees_integration_is_removed()
    {
        _currentUser.UserId = _employeeId;
        var other = EmployeeIntegration.Create(Guid.CreateVersion7(), IntegrationProvider.WhatsApp, "+5491100000000", Now);
        _integrations.Integrations.Add(other);
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(_employeeId, IntegrationProvider.WhatsApp, "+5491161972627", Now));

        await Handle();

        _integrations.Integrations.ShouldHaveSingleItem().ShouldBe(other);
    }

    [Fact]
    public async Task Unlinking_without_an_integration_is_not_found()
    {
        _currentUser.UserId = _employeeId;

        var result = await Handle();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_anonymous_request_is_forbidden()
    {
        _integrations.Integrations.Add(
            EmployeeIntegration.Create(_employeeId, IntegrationProvider.WhatsApp, "+5491161972627", Now));

        var result = await Handle();

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _integrations.Integrations.ShouldHaveSingleItem();
    }

    private Task<Result> Handle() =>
        new UnlinkWhatsAppHandler(_currentUser, _integrations, _unitOfWork, NullLogger<UnlinkWhatsAppHandler>.Instance)
            .Handle(new UnlinkWhatsAppCommand(), CancellationToken.None);
}
