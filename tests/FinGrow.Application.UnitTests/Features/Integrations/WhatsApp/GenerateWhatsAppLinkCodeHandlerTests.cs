namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.WhatsApp.GenerateWhatsAppLinkCode;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class GenerateWhatsAppLinkCodeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Employee _employee = CreateEmployee();

    public GenerateWhatsAppLinkCodeHandlerTests() => _employees.Employees.Add(_employee);

    [Fact]
    public async Task The_logged_in_employee_receives_a_code_whose_hash_is_persisted()
    {
        _currentUser.UserId = _employee.Id;

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Code.Length.ShouldBe(IntegrationLinkCode.Length);
        result.Value.ExpiresAt.ShouldBe(Now.Add(IntegrationLinkCode.Lifetime));
        var stored = _linkCodes.LinkCodes.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(_employee.Id);
        stored.Provider.ShouldBe(IntegrationProvider.WhatsApp);
        stored.CodeHash.ShouldBe(IntegrationLinkCode.Hash(result.Value.Code));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_anonymous_request_is_forbidden()
    {
        var result = await Handle();

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _linkCodes.LinkCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_user_who_is_not_an_active_employee_is_forbidden()
    {
        _currentUser.UserId = Guid.CreateVersion7();

        var result = await Handle();

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _linkCodes.LinkCodes.ShouldBeEmpty();
    }

    private Task<Result<WhatsAppLinkCodeResponse>> Handle()
    {
        var handler = new GenerateWhatsAppLinkCodeHandler(
            _currentUser, _employees, _linkCodes, _unitOfWork, new FakeDateTimeProvider(Now));

        return handler.Handle(new GenerateWhatsAppLinkCodeCommand(), CancellationToken.None);
    }

    private static Employee CreateEmployee() => Employee.Create(
        Guid.CreateVersion7(),
        departmentId: null,
        "Juan Perez",
        Email.From("juan.perez@empresa.com"),
        null,
        "hash-bcrypt",
        Currency.ARS,
        new DateOnly(2026, 1, 15),
        Now);
}
