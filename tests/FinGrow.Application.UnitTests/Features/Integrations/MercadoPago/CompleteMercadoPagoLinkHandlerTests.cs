namespace FinGrow.Application.UnitTests.Features.Integrations.MercadoPago;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Features.Integrations.MercadoPago.CompleteMercadoPagoLink;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class CompleteMercadoPagoLinkHandlerTests
{
    private const string Code = "TG-AUTH-CODE";
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly FakeMercadoPagoOAuthClient _oauth = new();
    private readonly Employee _employee = CreateEmployee();
    private readonly string _state = IntegrationLinkCode.GenerateCode();

    public CompleteMercadoPagoLinkHandlerTests()
    {
        _employees.Employees.Add(_employee);
        _linkCodes.LinkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.MercadoPago, _state, Now));
    }

    [Fact]
    public async Task A_valid_callback_links_the_account_and_stores_the_encrypted_grant()
    {
        var result = await Handle(Code, _state);

        result.IsSuccess.ShouldBeTrue();
        _oauth.ExchangedCodes.ShouldBe(new[] { Code });
        var integration = _integrations.Integrations.ShouldHaveSingleItem();
        integration.Provider.ShouldBe(IntegrationProvider.MercadoPago);
        integration.EmployeeId.ShouldBe(_employee.Id);
        integration.ExternalAccountId.ShouldBe(_oauth.Tokens.UserId);
        integration.Grant.ShouldNotBeNull();
        integration.Grant.AccessToken.ShouldBe("access-token");
        integration.Grant.RefreshToken.ShouldBe("refresh-token");
        integration.Grant.ExpiresAt.ShouldBe(Now.AddDays(180));
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Relinking_replaces_the_grant_of_the_existing_integration()
    {
        var existing = EmployeeIntegration.Create(_employee.Id, IntegrationProvider.MercadoPago, "111", Now.AddDays(-30));
        existing.Authorize(OAuthGrant.From("old-access", "old-refresh", Now), Now.AddDays(-30));
        _integrations.Integrations.Add(existing);

        var result = await Handle(Code, _state);

        result.IsSuccess.ShouldBeTrue();
        _integrations.Integrations.ShouldHaveSingleItem().ShouldBeSameAs(existing);
        existing.ExternalAccountId.ShouldBe(_oauth.Tokens.UserId);
        existing.Grant!.AccessToken.ShouldBe("access-token");
        existing.LinkedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task A_state_that_was_not_issued_by_fingrow_is_rejected_before_talking_to_mercado_pago()
    {
        var result = await Handle(Code, IntegrationLinkCode.GenerateCode());

        result.Error.ShouldBe(CompleteMercadoPagoLinkHandler.InvalidState);
        _oauth.ExchangedCodes.ShouldBeEmpty();
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_expired_state_is_rejected()
    {
        _clock.UtcNow = Now.Add(IntegrationLinkCode.Lifetime);

        var result = await Handle(Code, _state);

        result.Error.ShouldBe(CompleteMercadoPagoLinkHandler.InvalidState);
        _oauth.ExchangedCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_state_can_only_be_redeemed_once()
    {
        await Handle(Code, _state);

        var second = await Handle("ANOTHER-CODE", _state);

        second.Error.ShouldBe(CompleteMercadoPagoLinkHandler.InvalidState);
        _oauth.ExchangedCodes.ShouldBe(new[] { Code });
    }

    [Fact]
    public async Task When_mercado_pago_denies_access_nothing_is_linked()
    {
        var result = await Handle(code: null, _state, error: "access_denied");

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("Integrations.MercadoPago.Denied");
        _integrations.Integrations.ShouldBeEmpty();
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task When_the_token_exchange_fails_the_state_stays_usable_for_a_retry()
    {
        _oauth.Unreachable = true;

        var result = await Handle(Code, _state);

        result.Error.Type.ShouldBe(ErrorType.Failure);
        _integrations.Integrations.ShouldBeEmpty();
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    private Task<Result> Handle(string? code, string? state, string? error = null)
    {
        var linker = new LinkCodeRedeemer(
            _integrations, _linkCodes, _employees, _unitOfWork, _clock, NullLogger<LinkCodeRedeemer>.Instance);
        var handler = new CompleteMercadoPagoLinkHandler(
            linker, _oauth, _clock, NullLogger<CompleteMercadoPagoLinkHandler>.Instance);

        return handler.Handle(new CompleteMercadoPagoLinkCommand(code, state, error), CancellationToken.None);
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
