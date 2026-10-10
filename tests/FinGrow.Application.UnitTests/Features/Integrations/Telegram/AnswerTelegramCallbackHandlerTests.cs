namespace FinGrow.Application.UnitTests.Features.Integrations.Telegram;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Telegram;
using FinGrow.Application.Features.Integrations.Telegram.AnswerTelegramCallback;
using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class AnswerTelegramCallbackHandlerTests
{
    private const long ChatId = 123456789;
    private const long MessageId = 77;
    private const string CallbackQueryId = "callback-1";
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeEmployeeIntegrationRepository _integrations = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTelegramBotClient _bot = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    public AnswerTelegramCallbackHandlerTests() =>
        _integrations.Add(EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Telegram, "123456789", Now));

    [Fact]
    public async Task Choosing_a_payment_method_confirms_the_transaction_with_that_method()
    {
        var transaction = ProposedExpense();
        _clock.UtcNow = Now.AddMinutes(1);

        var result = await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm, PaymentMethod.DebitCard).Encode());

        result.IsSuccess.ShouldBeTrue();
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.PaymentMethod.ShouldBe(PaymentMethod.DebitCard);
        transaction.UpdatedAt.ShouldBe(Now.AddMinutes(1));
        _unitOfWork.SaveCount.ShouldBe(1);
        _bot.AnsweredCallbacks.ShouldHaveSingleItem().ShouldBe((CallbackQueryId, (string?)null));
        var edited = _bot.Edited.ShouldHaveSingleItem();
        edited.ChatId.ShouldBe(ChatId);
        edited.MessageId.ShouldBe(MessageId);
        edited.Text.ShouldStartWith("✅ Registrado: Gasto de $ 500 (ARS) en Alimentos (Débito)");
    }

    [Fact]
    public async Task Confirming_without_a_method_keeps_the_one_the_message_said()
    {
        var transaction = ProposedExpense(PaymentMethod.BankTransfer);

        await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm).Encode());

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.PaymentMethod.ShouldBe(PaymentMethod.BankTransfer);
    }

    [Fact]
    public async Task Discarding_leaves_the_transaction_discarded_and_says_nothing_was_registered()
    {
        var transaction = ProposedExpense();

        await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Discard).Encode());

        transaction.Status.ShouldBe(TransactionStatus.Discarded);
        _bot.Edited.ShouldHaveSingleItem().Text.ShouldBe(ChatTransactionText.DiscardedReply);
    }

    [Fact]
    public async Task A_transaction_already_reviewed_from_the_app_is_not_changed_from_the_chat()
    {
        var transaction = ProposedExpense();
        transaction.Confirm(Now);

        await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Discard).Encode());

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        _unitOfWork.SaveCount.ShouldBe(0);
        _bot.Edited.ShouldHaveSingleItem().Text.ShouldBe(ChatTransactionText.AlreadyReviewedReply);
    }

    [Fact]
    public async Task A_transaction_of_another_employee_cannot_be_reviewed()
    {
        var transaction = ProposedExpense(employeeId: Guid.CreateVersion7());

        await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm).Encode());

        transaction.Status.ShouldBe(TransactionStatus.Pending);
        _bot.Edited.ShouldHaveSingleItem().Text.ShouldBe(ChatTransactionText.AlreadyReviewedReply);
    }

    [Fact]
    public async Task Credit_card_is_ignored_when_confirming_an_income()
    {
        var income = Transaction.RegisterIncome(
            EmployeeId,
            Money.From(10000m, Currency.ARS),
            IncomeCategory.Freelance,
            "Freelance",
            new DateOnly(2026, 3, 15),
            PaymentMethod.Cash,
            TransactionSource.Telegram,
            TransactionStatus.Pending,
            Now);
        _transactions.Add(income);

        await Handle(new TelegramTransactionCallback(income.Id, ChatDecision.Confirm, PaymentMethod.CreditCard).Encode());

        income.Status.ShouldBe(TransactionStatus.Confirmed);
        income.PaymentMethod.ShouldBe(PaymentMethod.Cash);
    }

    [Fact]
    public async Task An_unknown_button_is_answered_without_touching_anything()
    {
        var transaction = ProposedExpense();

        await Handle("algo-viejo");

        transaction.Status.ShouldBe(TransactionStatus.Pending);
        _bot.AnsweredCallbacks.ShouldHaveSingleItem().Text.ShouldBe(AnswerTelegramCallbackHandler.UnknownButtonReply);
        _bot.Edited.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_button_from_an_unlinked_chat_does_not_review_anything()
    {
        var transaction = ProposedExpense();

        await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm).Encode(), chatId: 987654321);

        transaction.Status.ShouldBe(TransactionStatus.Pending);
        _bot.AnsweredCallbacks.ShouldHaveSingleItem().Text.ShouldBe(ReceiveTelegramMessageHandler.NotLinkedReply);
    }

    [Fact]
    public async Task Without_the_original_message_the_result_is_sent_as_a_new_message()
    {
        var transaction = ProposedExpense();

        await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Discard).Encode(), messageId: null);

        _bot.Edited.ShouldBeEmpty();
        _bot.Sent.ShouldHaveSingleItem().ShouldBe((ChatId, ChatTransactionText.DiscardedReply));
    }

    [Fact]
    public async Task The_review_is_kept_even_if_telegram_cannot_be_reached()
    {
        var transaction = ProposedExpense();
        _bot.Unreachable = true;

        var result = await Handle(new TelegramTransactionCallback(transaction.Id, ChatDecision.Confirm).Encode());

        result.IsSuccess.ShouldBeTrue();
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
    }

    private Transaction ProposedExpense(PaymentMethod paymentMethod = PaymentMethod.Cash, Guid? employeeId = null)
    {
        var transaction = Transaction.RegisterExpense(
            employeeId ?? EmployeeId,
            Money.From(500m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Supermercado",
            new DateOnly(2026, 3, 15),
            paymentMethod,
            TransactionSource.Telegram,
            TransactionStatus.Pending,
            Now,
            "telegram:123456789:42");
        _transactions.Add(transaction);

        return transaction;
    }

    private Task<Result> Handle(string data, long chatId = ChatId, long? messageId = MessageId)
    {
        var reviewer = new ChatTransactionReviewer(_transactions, _unitOfWork, _clock);
        var handler = new AnswerTelegramCallbackHandler(
            _integrations, reviewer, _bot, NullLogger<AnswerTelegramCallbackHandler>.Instance);

        return handler.Handle(new AnswerTelegramCallbackCommand(chatId, CallbackQueryId, messageId, data), CancellationToken.None);
    }
}
