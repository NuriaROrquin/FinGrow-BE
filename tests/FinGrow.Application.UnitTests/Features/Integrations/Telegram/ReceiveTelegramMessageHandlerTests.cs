namespace FinGrow.Application.UnitTests.Features.Integrations.Telegram;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class ReceiveTelegramMessageHandlerTests
{
    private const long ChatId = 123456789;
    private const string Code = "ABCD2345";
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeTelegramBotClient _bot = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly Employee _employee = CreateEmployee();

    public ReceiveTelegramMessageHandlerTests() => _employees.Employees.Add(_employee);

    [Fact]
    public async Task An_unlinked_chat_sending_plain_text_is_told_how_to_link()
    {
        var result = await Handle("gasté 500 pesos en el super");

        result.IsSuccess.ShouldBeTrue();
        _bot.Sent.ShouldHaveSingleItem().ShouldBe((ChatId, ReceiveTelegramMessageHandler.NotLinkedReply));
        _integrations.Integrations.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_bare_start_command_is_told_how_to_link()
    {
        await Handle("/start");

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ReceiveTelegramMessageHandler.NotLinkedReply);
    }

    [Fact]
    public async Task A_valid_code_links_the_chat_to_the_employee_who_generated_it()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));

        var result = await Handle(" abcd 2345 ");

        result.IsSuccess.ShouldBeTrue();
        _bot.Sent.ShouldHaveSingleItem().Text.ShouldStartWith("Listo, Juan Perez");
        var integration = _integrations.Integrations.ShouldHaveSingleItem();
        integration.Provider.ShouldBe(IntegrationProvider.Telegram);
        integration.EmployeeId.ShouldBe(_employee.Id);
        integration.ExternalAccountId.ShouldBe("123456789");
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_code_sent_through_the_start_deep_link_also_links_the_chat()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));

        await Handle("/start " + Code);

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldStartWith("Listo, Juan Perez");
        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe("123456789");
    }

    [Fact]
    public async Task A_whatsapp_code_does_not_link_a_telegram_chat()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        await Handle(Code);

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ReceiveTelegramMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_code_cannot_be_used_twice()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));
        await Handle(Code);

        await Handle(Code, chatId: 987654321);

        _bot.Sent.Last().ShouldBe((987654321, ReceiveTelegramMessageHandler.InvalidCodeReply));
        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe("123456789");
    }

    [Fact]
    public async Task An_expired_code_is_rejected()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));
        _clock.UtcNow = Now.Add(IntegrationLinkCode.Lifetime).AddSeconds(1);

        await Handle(Code);

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ReceiveTelegramMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linking_from_a_new_chat_replaces_the_previous_one()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.Telegram, "111", Now));
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));

        await Handle(Code);

        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe("123456789");
    }

    [Fact]
    public async Task A_linked_chat_is_resolved_to_its_employee()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.Telegram, "123456789", Now));

        var result = await Handle("gasté 500 en el super");

        result.IsSuccess.ShouldBeTrue();
        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ReceiveTelegramMessageHandler.MessageReceivedReply);
    }

    [Fact]
    public async Task A_linked_chat_sending_a_code_is_not_relinked()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.Telegram, "123456789", Now));
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));

        await Handle(Code);

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ReceiveTelegramMessageHandler.MessageReceivedReply);
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task The_link_is_kept_even_if_the_reply_cannot_be_delivered()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));
        _bot.Unreachable = true;

        var result = await Handle(Code);

        result.IsSuccess.ShouldBeTrue();
        _integrations.Integrations.ShouldHaveSingleItem();
        _bot.Sent.ShouldBeEmpty();
    }

    private Task<Result> Handle(string text, long chatId = ChatId)
    {
        var linker = new LinkCodeRedeemer(
            _integrations, _linkCodes, _employees, _unitOfWork, _clock, NullLogger<LinkCodeRedeemer>.Instance);
        var handler = new ReceiveTelegramMessageHandler(
            _integrations, linker, _bot, NullLogger<ReceiveTelegramMessageHandler>.Instance);

        return handler.Handle(new ReceiveTelegramMessageCommand(chatId, text, 42), CancellationToken.None);
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
