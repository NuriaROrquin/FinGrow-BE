namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class ReceiveWhatsAppMessageHandlerTests
{
    private const string Phone = "+5491112345678";
    private const string From = "whatsapp:" + Phone;
    private const string Code = "ABCD2345";
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeTwilioMediaClient _media = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly Employee _employee = CreateEmployee();

    public ReceiveWhatsAppMessageHandlerTests() => _employees.Employees.Add(_employee);

    [Fact]
    public async Task An_unlinked_number_sending_plain_text_is_told_how_to_link()
    {
        var result = await Handle(From, "gasté 500 pesos en el super");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.NotLinkedReply);
        _integrations.Integrations.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_valid_code_links_the_number_to_the_employee_who_generated_it()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(From, " abcd 2345 ");

        result.Value.Text.ShouldStartWith("Listo, Juan Perez");
        var integration = _integrations.Integrations.ShouldHaveSingleItem();
        integration.EmployeeId.ShouldBe(_employee.Id);
        integration.ExternalAccountId.ShouldBe(Phone);
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_code_cannot_be_used_twice()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));
        await Handle(From, Code);

        var result = await Handle("whatsapp:+5491199999999", Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe(Phone);
    }

    [Fact]
    public async Task An_expired_code_is_rejected()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));
        _clock.UtcNow = Now.Add(IntegrationLinkCode.Lifetime).AddSeconds(1);

        var result = await Handle(From, Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_unknown_code_is_rejected()
    {
        var result = await Handle(From, "ZZZZ9999");

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
    }

    [Fact]
    public async Task A_code_of_a_deactivated_employee_is_rejected()
    {
        _employee.Deactivate(Now);
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(From, Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linking_from_a_new_phone_replaces_the_previous_one()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.WhatsApp, "+5491100000000", Now));
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(From, Code);

        result.Value.Text.ShouldStartWith("Listo, Juan Perez");
        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe(Phone);
    }

    [Fact]
    public async Task A_linked_number_is_resolved_to_its_employee_and_its_attachments_are_downloaded()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.WhatsApp, Phone, Now));
        var audio = new Uri("https://api.twilio.com/2010-04-01/Accounts/AC1/Messages/MM1/Media/ME1");

        var result = await Handle(From, string.Empty, new WhatsAppInboundMedia(audio, "audio/ogg"));

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.MessageReceivedReply);
        _media.Downloaded.ShouldBe(new[] { audio });
    }

    [Fact]
    public async Task A_linked_number_sending_a_code_is_not_relinked()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.WhatsApp, Phone, Now));
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(From, Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.MessageReceivedReply);
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    private Task<Result<WhatsAppReply>> Handle(string from, string body, params WhatsAppInboundMedia[] media)
    {
        var linker = new LinkCodeRedeemer(
            _integrations, _linkCodes, _employees, _unitOfWork, _clock, NullLogger<LinkCodeRedeemer>.Instance);
        var handler = new ReceiveWhatsAppMessageHandler(
            _integrations, linker, _media, NullLogger<ReceiveWhatsAppMessageHandler>.Instance);

        return handler.Handle(new ReceiveWhatsAppMessageCommand(from, body, "SM123", media), CancellationToken.None);
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
