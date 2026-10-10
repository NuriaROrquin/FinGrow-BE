namespace FinGrow.Application.UnitTests.Features.Integrations.Telegram;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Features.Integrations.Telegram;
using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;
using FinGrow.Application.Interfaces;
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
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeAiService _ai = new();
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
    public async Task A_linked_chat_sending_a_code_is_not_relinked()
    {
        LinkChat();
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.Telegram, Code, Now));

        await Handle(Code);

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ChatTransactionText.HelpReply);
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task An_expense_written_in_natural_language_is_proposed_as_a_pending_telegram_transaction()
    {
        LinkChat();
        _ai.ParseResult = Parsed(Expense(paymentMethod: null));

        var result = await Handle("Gasté $500 en supermercado");

        result.IsSuccess.ShouldBeTrue();
        _ai.ParsedMessages.ShouldHaveSingleItem().ShouldBe(("Gasté $500 en supermercado", Currency.ARS));
        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.EmployeeId.ShouldBe(_employee.Id);
        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.Amount.ShouldBe(Money.From(500m, Currency.ARS));
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        transaction.Description.ShouldBe("Supermercado");
        transaction.OccurredOn.ShouldBe(new DateOnly(2026, 3, 15));
        transaction.Source.ShouldBe(TransactionSource.Telegram);
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.PaymentMethod.ShouldBe(ChatTransactionProposer.DefaultPaymentMethod);
        transaction.ExternalReference.ShouldBe("telegram:123456789:42");
        transaction.AiConfidence.ShouldBe(0.9);
        transaction.AiModel.ShouldBe("fake-model");
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_proposal_shows_type_amount_and_category_and_asks_how_it_was_paid()
    {
        LinkChat();
        _ai.ParseResult = Parsed(Expense(paymentMethod: null));

        await Handle("Gasté $500 en supermercado");

        var (chatId, text, buttonRows) = _bot.SentWithButtons.ShouldHaveSingleItem();
        chatId.ShouldBe(ChatId);
        text.ShouldContain("Gasto: $ 500 (ARS)");
        text.ShouldContain("Categoría: Alimentos");
        text.ShouldContain("¿Cómo lo pagaste?");
        var transactionId = _transactions.Transactions.Single().Id;
        var callbacks = buttonRows.SelectMany(row => row)
            .Select(button => TelegramTransactionCallback.Parse(button.CallbackData).ShouldNotBeNull())
            .ToList();
        callbacks.ShouldAllBe(callback => callback.TransactionId == transactionId);
        callbacks.Where(callback => callback.Decision == ChatDecision.Confirm)
            .Select(callback => callback.PaymentMethod)
            .ShouldBe(new PaymentMethod?[]
            {
                PaymentMethod.Cash, PaymentMethod.DebitCard, PaymentMethod.CreditCard, PaymentMethod.BankTransfer, PaymentMethod.DigitalWallet
            });
        callbacks.Last().Decision.ShouldBe(ChatDecision.Discard);
    }

    [Fact]
    public async Task When_the_message_says_how_it_was_paid_only_confirm_and_discard_are_offered()
    {
        LinkChat();
        _ai.ParseResult = Parsed(Expense(paymentMethod: PaymentMethod.DebitCard));

        await Handle("Pagué $500 en el super con débito");

        _transactions.Transactions.Single().PaymentMethod.ShouldBe(PaymentMethod.DebitCard);
        var (_, text, buttonRows) = _bot.SentWithButtons.ShouldHaveSingleItem();
        text.ShouldContain("Débito");
        text.ShouldContain("¿Lo registro?");
        buttonRows.ShouldHaveSingleItem().Select(button => button.Text)
            .ShouldBe(new[] { ChatTransactionText.ConfirmLabel, ChatTransactionText.DiscardLabel });
    }

    [Fact]
    public async Task An_income_is_proposed_with_its_income_category_and_never_offers_credit_card()
    {
        LinkChat();
        _ai.ParseResult = Parsed(new ParsedTransaction(
            TransactionType.Income, 10000m, Currency.ARS, null, IncomeCategory.Freelance, "Freelance", PaymentMethod.CreditCard, 0.85));

        await Handle("Ingreso de $10000 por freelance");

        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Type.ShouldBe(TransactionType.Income);
        transaction.IncomeCategory.ShouldBe(IncomeCategory.Freelance);
        transaction.AiConfidence.ShouldBe(0.85);
        var (_, text, buttonRows) = _bot.SentWithButtons.ShouldHaveSingleItem();
        text.ShouldContain("Ingreso: $ 10.000 (ARS)");
        text.ShouldContain("¿Cómo lo cobraste?");
        buttonRows.SelectMany(row => row)
            .Select(button => TelegramTransactionCallback.Parse(button.CallbackData)!.PaymentMethod)
            .ShouldNotContain(PaymentMethod.CreditCard);
    }

    [Theory]
    [InlineData(MessageParsingOutcome.MissingAmount, ChatTransactionText.MissingAmountReply)]
    [InlineData(MessageParsingOutcome.MultipleTransactions, ChatTransactionText.MultipleTransactionsReply)]
    [InlineData(MessageParsingOutcome.NotATransaction, ChatTransactionText.HelpReply)]
    public async Task A_message_that_cannot_be_registered_is_answered_without_creating_a_transaction(
        MessageParsingOutcome outcome,
        string expectedReply)
    {
        LinkChat();
        _ai.ParseResult = ParsedMessage.Of(outcome);

        var result = await Handle("Gasté en el super");

        result.IsSuccess.ShouldBeTrue();
        _bot.Sent.ShouldHaveSingleItem().ShouldBe((ChatId, expectedReply));
        _bot.SentWithButtons.ShouldBeEmpty();
        _transactions.Transactions.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task When_the_AI_is_down_the_employee_is_told_to_try_again_and_nothing_is_registered()
    {
        LinkChat();
        _ai.Unreachable = true;

        var result = await Handle("Gasté $500 en supermercado");

        result.IsSuccess.ShouldBeTrue();
        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ChatTransactionText.AiUnavailableReply);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_message_delivered_twice_by_telegram_is_proposed_only_once()
    {
        LinkChat();
        _ai.ParseResult = Parsed(Expense(paymentMethod: null));

        await Handle("Gasté $500 en supermercado");
        await Handle("Gasté $500 en supermercado");

        _transactions.Transactions.ShouldHaveSingleItem();
        _ai.ParsedMessages.ShouldHaveSingleItem();
        _bot.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_command_from_a_linked_chat_gets_the_examples_without_asking_the_AI()
    {
        LinkChat();

        await Handle("/start");

        _bot.Sent.ShouldHaveSingleItem().Text.ShouldBe(ChatTransactionText.HelpReply);
        _ai.ParsedMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_transaction_is_kept_even_if_the_proposal_cannot_be_delivered()
    {
        LinkChat();
        _ai.ParseResult = Parsed(Expense(paymentMethod: null));
        _bot.Unreachable = true;

        var result = await Handle("Gasté $500 en supermercado");

        result.IsSuccess.ShouldBeTrue();
        _transactions.Transactions.ShouldHaveSingleItem().IsPending.ShouldBeTrue();
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
        var proposer = new ChatTransactionProposer(
            _ai, _transactions, _unitOfWork, _clock, NullLogger<ChatTransactionProposer>.Instance);
        var handler = new ReceiveTelegramMessageHandler(
            _integrations, linker, proposer, _bot, NullLogger<ReceiveTelegramMessageHandler>.Instance);

        return handler.Handle(new ReceiveTelegramMessageCommand(chatId, text, 42), CancellationToken.None);
    }

    private void LinkChat() =>
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.Telegram, "123456789", Now));

    private static ParsedTransaction Expense(PaymentMethod? paymentMethod) => new(
        TransactionType.Expense, 500m, Currency.ARS, ExpenseCategory.Alimentos, null, "Supermercado", paymentMethod, 0.9);

    private static ParsedMessage Parsed(ParsedTransaction transaction) =>
        new(MessageParsingOutcome.Parsed, transaction, "fake-model");

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
