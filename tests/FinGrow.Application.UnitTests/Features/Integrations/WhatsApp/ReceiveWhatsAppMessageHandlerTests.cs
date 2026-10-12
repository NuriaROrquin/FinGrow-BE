namespace FinGrow.Application.UnitTests.Features.Integrations.WhatsApp;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Features.Integrations.WhatsApp;
using FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class ReceiveWhatsAppMessageHandlerTests
{
    private const string Phone = "+5491112345678";
    private const string Code = "ABCD2345";
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly WhatsAppInboundMedia VoiceNote =
        new(new Uri("https://api.twilio.com/2010-04-01/Accounts/AC1/Messages/MM1/Media/ME1"), "audio/ogg");

    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeIntegrationLinkCodeRepository _linkCodes = new();
    private readonly FakeTwilioMediaClient _media = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeAiService _ai = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly Employee _employee = CreateEmployee();

    public ReceiveWhatsAppMessageHandlerTests() => _employees.Employees.Add(_employee);

    [Fact]
    public async Task An_unlinked_number_sending_plain_text_is_told_how_to_link()
    {
        var result = await Handle(Phone, "gasté 500 pesos en el super");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.NotLinkedReply);
        _integrations.Integrations.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_valid_code_links_the_number_to_the_employee_who_generated_it()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(Phone, " abcd 2345 ");

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
        await Handle(Phone, Code);

        var result = await Handle("+5491199999999", Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe(Phone);
    }

    [Fact]
    public async Task An_expired_code_is_rejected()
    {
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));
        _clock.UtcNow = Now.Add(IntegrationLinkCode.Lifetime).AddSeconds(1);

        var result = await Handle(Phone, Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_unknown_code_is_rejected()
    {
        var result = await Handle(Phone, "ZZZZ9999");

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
    }

    [Fact]
    public async Task A_code_of_a_deactivated_employee_is_rejected()
    {
        _employee.Deactivate(Now);
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(Phone, Code);

        result.Value.Text.ShouldBe(ReceiveWhatsAppMessageHandler.InvalidCodeReply);
        _integrations.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linking_from_a_new_phone_replaces_the_previous_one()
    {
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.WhatsApp, "+5491100000000", Now));
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(Phone, Code);

        result.Value.Text.ShouldStartWith("Listo, Juan Perez");
        _integrations.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe(Phone);
    }

    [Fact]
    public async Task A_linked_number_writing_an_expense_gets_the_proposal_and_it_stays_pending()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.DebitCard));

        var result = await Handle(Phone, "Gasté $500 en el super con débito");

        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.EmployeeId.ShouldBe(_employee.Id);
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.Source.ShouldBe(TransactionSource.WhatsApp);
        transaction.ExternalReference.ShouldBe("whatsapp:+5491112345678:SM123");
        transaction.PaymentMethod.ShouldBe(PaymentMethod.DebitCard);
        result.Value.Text.ShouldBe(WhatsAppTransactionText.Proposal(transaction, asksPaymentMethod: false));
        _ai.ParsedMessages.ShouldHaveSingleItem().Text.ShouldBe("Gasté $500 en el super con débito");
    }

    [Fact]
    public async Task Without_a_payment_method_the_proposal_asks_for_it_with_numbers()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(paymentMethod: null));

        var result = await Handle(Phone, "Gasté $500 en el super");

        result.Value.Text!.ShouldContain("¿Cómo lo pagaste? Respondé con el número:");
        _transactions.Transactions.ShouldHaveSingleItem().Status.ShouldBe(TransactionStatus.Pending);
    }

    [Theory]
    [InlineData("SÍ")]
    [InlineData("dale")]
    [InlineData("👍")]
    public async Task Answering_yes_confirms_the_whatsapp_proposal(string answer)
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.DebitCard));
        await Handle(Phone, "Gasté $500 en el super con débito");

        var result = await HandleMessage(Phone, answer, "SM124");

        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        result.Value.Text.ShouldBe(ChatTransactionText.Confirmed(transaction));
        _ai.ParsedMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Answering_no_discards_the_proposal()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.Cash));
        await Handle(Phone, "Gasté $500 en el super");

        var result = await HandleMessage(Phone, "No", "SM124");

        _transactions.Transactions.ShouldHaveSingleItem().Status.ShouldBe(TransactionStatus.Discarded);
        result.Value.Text.ShouldBe(ChatTransactionText.DiscardedReply);
    }

    [Fact]
    public async Task Answering_a_number_sets_that_payment_method_and_confirms()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(paymentMethod: null));
        await Handle(Phone, "Gasté $500 en el super");

        await HandleMessage(Phone, "3", "SM124");

        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.PaymentMethod.ShouldBe(PaymentMethod.CreditCard);
    }

    [Fact]
    public async Task A_number_outside_the_options_is_handled_as_a_new_message()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(new ParsedTransaction(
            TransactionType.Income, 10000m, Currency.ARS, null, IncomeCategory.Freelance, "Freelance", null, 0.9));
        await Handle(Phone, "Cobré $10000 por un freelance");
        _ai.ParseResult = ParsedMessage.Of(MessageParsingOutcome.NotATransaction);

        await HandleMessage(Phone, "4", "SM124");

        _transactions.Transactions.ShouldHaveSingleItem().Status.ShouldBe(TransactionStatus.Pending);
        _ai.ParsedMessages.Last().Text.ShouldBe("4");
    }

    [Fact]
    public async Task Yes_confirms_the_most_recent_proposal_when_there_are_several()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.Cash));
        await HandleMessage(Phone, "Gasté $500 en el super", "SM1");
        _clock.UtcNow = Now.AddMinutes(5);
        await HandleMessage(Phone, "Gasté $500 en el kiosco", "SM2");

        await HandleMessage(Phone, "sí", "SM3");

        ProposalOf("SM2").Status.ShouldBe(TransactionStatus.Confirmed);
        ProposalOf("SM1").Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Yes_without_a_recent_whatsapp_proposal_is_handled_as_a_new_message()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.Cash));
        await Handle(Phone, "Gasté $500 en el super");
        _clock.UtcNow = Now.Add(WhatsAppProposalReviewer.ProposalWindow).AddMinutes(1);
        _ai.ParseResult = ParsedMessage.Of(MessageParsingOutcome.NotATransaction);

        var result = await HandleMessage(Phone, "sí", "SM124");

        result.Value.Text.ShouldBe(ChatTransactionText.HelpReply);
        _transactions.Transactions.ShouldHaveSingleItem().Status.ShouldBe(TransactionStatus.Pending);
        _ai.ParsedMessages.Last().Text.ShouldBe("sí");
    }

    [Fact]
    public async Task Yes_does_not_confirm_a_proposal_from_another_channel()
    {
        LinkPhone();
        var telegramProposal = Transaction.RegisterExpense(
            _employee.Id,
            Money.From(500m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Supermercado",
            new DateOnly(2026, 3, 15),
            PaymentMethod.Cash,
            TransactionSource.Telegram,
            TransactionStatus.Pending,
            Now,
            "telegram:123456789:42");
        _transactions.Add(telegramProposal);

        await Handle(Phone, "sí");

        telegramProposal.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public async Task A_message_twilio_delivers_twice_is_proposed_once_and_the_retry_gets_no_reply()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.Cash));
        await Handle(Phone, "Gasté $500 en el super");

        var retry = await Handle(Phone, "Gasté $500 en el super");

        retry.Value.ShouldBe(WhatsAppReply.Silent);
        _transactions.Transactions.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(MessageParsingOutcome.MissingAmount, ChatTransactionText.MissingAmountReply)]
    [InlineData(MessageParsingOutcome.MultipleTransactions, ChatTransactionText.MultipleTransactionsReply)]
    [InlineData(MessageParsingOutcome.NotATransaction, ChatTransactionText.HelpReply)]
    public async Task A_message_that_cannot_be_registered_is_answered_without_creating_a_transaction(
        MessageParsingOutcome outcome,
        string expectedReply)
    {
        LinkPhone();
        _ai.ParseResult = ParsedMessage.Of(outcome);

        var result = await Handle(Phone, "Gasté en el super");

        result.Value.Text.ShouldBe(expectedReply);
        _transactions.Transactions.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task When_the_AI_is_down_the_employee_is_told_to_try_again()
    {
        LinkPhone();
        _ai.Unreachable = true;

        var result = await Handle(Phone, "Gasté $500 en el super");

        result.Value.Text.ShouldBe(ChatTransactionText.AiUnavailableReply);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_message_without_text_or_audio_gets_the_help()
    {
        LinkPhone();
        var image = new WhatsAppInboundMedia(new Uri("https://api.twilio.com/2010-04-01/Accounts/AC1/Messages/MM1/Media/ME1"), "image/jpeg");

        var result = await Handle(Phone, string.Empty, image);

        result.Value.Text.ShouldBe(ChatTransactionText.HelpReply);
        _media.Downloaded.ShouldBeEmpty();
        _ai.ParsedMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_linked_number_sending_a_code_is_not_relinked()
    {
        LinkPhone();
        _linkCodes.Add(IntegrationLinkCode.Create(_employee.Id, IntegrationProvider.WhatsApp, Code, Now));

        var result = await Handle(Phone, Code);

        result.Value.Text.ShouldBe(ChatTransactionText.HelpReply);
        _linkCodes.LinkCodes.Single().UsedAt.ShouldBeNull();
    }

    [Fact]
    public async Task A_voice_note_is_transcribed_and_proposed_like_a_written_message()
    {
        LinkPhone();
        _ai.TranscriptionResult = Transcribed("Gasté 2500 pesos en el súper con débito");
        _ai.ParseResult = Parsed(Expense(PaymentMethod.DebitCard));

        var result = await Handle(Phone, string.Empty, VoiceNote);

        _media.Downloaded.ShouldBe(new[] { VoiceNote.Url });
        _ai.TranscribedAudios.ShouldHaveSingleItem().ContentType.ShouldBe("audio/ogg");
        _ai.ParsedMessages.ShouldHaveSingleItem().Text.ShouldBe("Gasté 2500 pesos en el súper con débito");
        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.ExternalReference.ShouldBe("whatsapp:+5491112345678:SM123");
        result.Value.Text.ShouldBe(
            "🎙️ Entendí: «Gasté 2500 pesos en el súper con débito»\n\n" +
            WhatsAppTransactionText.Proposal(transaction, asksPaymentMethod: false));
    }

    [Fact]
    public async Task A_voice_note_without_an_amount_shows_what_was_heard_and_asks_again()
    {
        LinkPhone();
        _ai.TranscriptionResult = Transcribed("Gasté en el súper");
        _ai.ParseResult = ParsedMessage.Of(MessageParsingOutcome.MissingAmount);

        var result = await Handle(Phone, string.Empty, VoiceNote);

        result.Value.Text.ShouldBe("🎙️ Entendí: «Gasté en el súper»\n\n" + ChatTransactionText.MissingAmountReply);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(AudioTranscriptionOutcome.NotUnderstood)]
    [InlineData(AudioTranscriptionOutcome.UnsupportedFormat)]
    [InlineData(AudioTranscriptionOutcome.TooLarge)]
    public async Task A_voice_note_that_cannot_be_transcribed_asks_to_repeat_it_or_write_it(AudioTranscriptionOutcome outcome)
    {
        LinkPhone();
        _ai.TranscriptionResult = AudioTranscription.Of(outcome);

        var result = await Handle(Phone, string.Empty, VoiceNote);

        result.Value.Text.ShouldBe(ChatTransactionText.AudioNotUnderstoodReply);
        _ai.ParsedMessages.ShouldBeEmpty();
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_voice_note_cannot_be_downloaded_the_employee_is_told_to_try_again()
    {
        LinkPhone();
        _media.Unreachable = true;

        var result = await Handle(Phone, string.Empty, VoiceNote);

        result.Value.Text.ShouldBe(ChatTransactionText.AiUnavailableReply);
        _ai.TranscribedAudios.ShouldBeEmpty();
    }

    [Fact]
    public async Task When_the_transcription_service_is_down_the_employee_is_told_to_try_again()
    {
        LinkPhone();
        _ai.Unreachable = true;

        var result = await Handle(Phone, string.Empty, VoiceNote);

        result.Value.Text.ShouldBe(ChatTransactionText.AiUnavailableReply);
        _transactions.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_voice_note_saying_yes_confirms_the_pending_proposal()
    {
        LinkPhone();
        _ai.ParseResult = Parsed(Expense(PaymentMethod.Cash));
        await Handle(Phone, "Gasté $500 en el super");
        _ai.TranscriptionResult = Transcribed("Sí.");

        var result = await HandleMessage(Phone, string.Empty, "SM124", VoiceNote);

        var transaction = _transactions.Transactions.ShouldHaveSingleItem();
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        result.Value.Text.ShouldBe("🎙️ Entendí: «Sí.»\n\n" + ChatTransactionText.Confirmed(transaction));
    }

    [Fact]
    public async Task When_processing_takes_longer_than_twilio_waits_the_employee_is_told_to_try_again()
    {
        LinkPhone();
        _ai.TranscriptionHangs = true;

        var result = await Handle(Phone, string.Empty, VoiceNote);

        result.Value.Text.ShouldBe(ChatTransactionText.AiUnavailableReply);
        _transactions.Transactions.ShouldBeEmpty();
    }

    private Task<Result<WhatsAppReply>> Handle(string phoneNumber, string body, params WhatsAppInboundMedia[] media) =>
        HandleMessage(phoneNumber, body, "SM123", media);

    private Task<Result<WhatsAppReply>> HandleMessage(
        string phoneNumber,
        string body,
        string messageSid,
        params WhatsAppInboundMedia[] media)
    {
        var linker = new LinkCodeRedeemer(
            _integrations, _linkCodes, _employees, _unitOfWork, _clock, NullLogger<LinkCodeRedeemer>.Instance);
        var voiceNotes = new ChatVoiceNoteTranscriber(_ai, NullLogger<ChatVoiceNoteTranscriber>.Instance);
        var proposer = new ChatTransactionProposer(
            _ai, _transactions, _unitOfWork, _clock, NullLogger<ChatTransactionProposer>.Instance);
        var reviewer = new WhatsAppProposalReviewer(
            _transactions, new ChatTransactionReviewer(_transactions, _unitOfWork, _clock), _clock);
        var handler = new ReceiveWhatsAppMessageHandler(
            _integrations, linker, _media, voiceNotes, proposer, reviewer, NullLogger<ReceiveWhatsAppMessageHandler>.Instance)
        {
            ProcessingBudget = TimeSpan.FromMilliseconds(200)
        };

        return handler.Handle(new ReceiveWhatsAppMessageCommand(phoneNumber, body, messageSid, media), CancellationToken.None);
    }

    private static AudioTranscription Transcribed(string text) =>
        new(AudioTranscriptionOutcome.Transcribed, text, "fake-transcribe");

    private void LinkPhone() =>
        _integrations.Add(EmployeeIntegration.Create(_employee.Id, IntegrationProvider.WhatsApp, Phone, Now));

    private Transaction ProposalOf(string messageSid) =>
        _transactions.Transactions.Single(transaction =>
            transaction.ExternalReference == ReceiveWhatsAppMessageHandler.ExternalReference(Phone, messageSid));

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
