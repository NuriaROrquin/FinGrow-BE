namespace FinGrow.Application.UnitTests.Features.Integrations.Gmail;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Gmail.CompleteGmailLink;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class CompleteGmailLinkHandlerTests
{
    private const string Code = "4/google-auth-code";
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly FakeGmailOAuthClient _oauth = new();
    private readonly Employee _employee = CreateEmployee();
    private readonly string _state = IntegrationLinkCode.GenerateCode();

    public CompleteGmailLinkHandlerTests()
    {
        _employees.Employees.Add(_employee);
        _linkCodes.LinkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Gmail, _state, Now));
    }

    [Fact]
    public async Task A_valid_callback_links_the_mailbox_as_active_and_stores_the_grant()
    {
        var result = await Handle(Code, _state);

        result.IsSuccess.ShouldBeTrue();
        _oauth.ExchangedCodes.ShouldBe(new[] { Code });
        var integration = _integrations.Integrations.ShouldHaveSingleItem();
        integration.Provider.ShouldBe(IntegrationProvider.Gmail);
        integration.EmployeeId.ShouldBe(_employee.Id);
        integration.ExternalAccountId.ShouldBe("juan.perez@gmail.com");
        integration.Status.ShouldBe(IntegrationStatus.Active);
        integration.Grant.ShouldNotBeNull();
        integration.Grant.AccessToken.ShouldBe("access-token");
        integration.Grant.RefreshToken.ShouldBe("refresh-token");
        integration.Grant.ExpiresAt.ShouldBe(Now.AddHours(1));
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Relinking_a_failed_integration_makes_it_active_again()
    {
        var existing = EmployeeIntegration.Create(_employee.Id, IntegrationProvider.Gmail, "juan.perez@gmail.com", Now.AddDays(-30));
        existing.Authorize(OAuthGrant.From("old-access", "old-refresh", Now), Now.AddDays(-30));
        existing.MarkFailed(Now.AddDays(-1));
        _integrations.Integrations.Add(existing);

        var result = await Handle(Code, _state);

        result.IsSuccess.ShouldBeTrue();
        _integrations.Integrations.ShouldHaveSingleItem().ShouldBeSameAs(existing);
        existing.Status.ShouldBe(IntegrationStatus.Active);
        existing.Grant!.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task A_state_that_was_not_issued_by_fingrow_is_rejected_before_talking_to_google()
    {
        var result = await Handle(Code, IntegrationLinkCode.GenerateCode());

        result.Error.ShouldBe(CompleteGmailLinkHandler.InvalidState);
        _oauth.ExchangedCodes.ShouldBeEmpty();
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_state_issued_for_another_provider_is_rejected()
    {
        var mercadoPagoState = IntegrationLinkCode.GenerateCode();
        _linkCodes.LinkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.MercadoPago, mercadoPagoState, Now));

        var result = await Handle(Code, mercadoPagoState);

        result.Error.ShouldBe(CompleteGmailLinkHandler.InvalidState);
        _oauth.ExchangedCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_employee_denies_access_nothing_is_linked()
    {
        var result = await Handle(code: null, _state, error: "access_denied");

        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("Integrations.Gmail.Denied");
        _integrations.Integrations.ShouldBeEmpty();
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task When_the_employee_unticks_mail_reading_nothing_is_linked_and_the_state_stays_usable()
    {
        _oauth.Tokens = _oauth.Tokens with { CanReadMail = false };

        var result = await Handle(Code, _state);

        result.Error.ShouldBe(CompleteGmailLinkHandler.MailAccessNotGranted);
        _integrations.Integrations.ShouldBeEmpty();
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_refresh_token_nothing_is_linked()
    {
        _oauth.Tokens = _oauth.Tokens with { RefreshToken = null };

        var result = await Handle(Code, _state);

        result.Error.ShouldBe(CompleteGmailLinkHandler.ExchangeFailed);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_token_exchange_fails_the_state_stays_usable_for_a_retry()
    {
        _oauth.Unreachable = true;

        var result = await Handle(Code, _state);

        result.Error.ShouldBe(CompleteGmailLinkHandler.ExchangeFailed);
        _integrations.Integrations.ShouldBeEmpty();
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    private Task<Result> Handle(string? code, string? state, string? error = null)
    {
        var linker = new LinkCodeRedeemer(
            _integrations, _linkCodes, _employees, _unitOfWork, _clock, NullLogger<LinkCodeRedeemer>.Instance);
        var handler = new CompleteGmailLinkHandler(
            linker, _oauth, _clock, NullLogger<CompleteGmailLinkHandler>.Instance);

        return handler.Handle(new CompleteGmailLinkCommand(code, state, error), CancellationToken.None);
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
