namespace FinGrow.Application.UnitTests.Features.Integrations.MercadoPago;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Features.Integrations.MercadoPago.StartMercadoPagoLink;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class StartMercadoPagoLinkHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeMercadoPagoOAuthClient _oauth = new();
    private readonly Employee _employee = CreateEmployee();

    public StartMercadoPagoLinkHandlerTests() => _employees.Employees.Add(_employee);

    [Fact]
    public async Task The_authorization_url_carries_a_fresh_link_code_as_state()
    {
        _currentUser.UserId = _employee.Id;

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        var stored = _linkCodes.LinkCodes.ShouldHaveSingleItem();
        stored.Provider.ShouldBe(IntegrationProvider.MercadoPago);
        stored.EmployeeId.ShouldBe(_employee.Id);
        var state = result.Value.AuthorizationUrl.Query["?state=".Length..];
        stored.CodeHash.ShouldBe(IntegrationLinkCode.Hash(state));
        result.Value.ExpiresAt.ShouldBe(Now.Add(IntegrationLinkCode.Lifetime));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_anonymous_request_is_forbidden_and_issues_nothing()
    {
        var result = await Handle();

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _linkCodes.LinkCodes.ShouldBeEmpty();
    }

    private Task<Result<StartMercadoPagoLinkResponse>> Handle()
    {
        var issuer = new LinkCodeIssuer(_currentUser, _employees, _linkCodes, _unitOfWork, new FakeDateTimeProvider(Now));
        var handler = new StartMercadoPagoLinkHandler(issuer, _oauth);

        return handler.Handle(new StartMercadoPagoLinkCommand(), CancellationToken.None);
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
