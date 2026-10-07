namespace FinGrow.Application.UnitTests.Features.Transactions.PendingReview;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Transactions.ConfirmTransaction;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class ConfirmTransactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Confirming_without_changes_makes_the_proposal_count()
    {
        var proposal = AddPendingExpense();

        var result = await Handle(CommandFrom(proposal));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(TransactionStatus.Confirmed);
        proposal.Status.ShouldBe(TransactionStatus.Confirmed);
        proposal.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Confirming_with_corrections_stores_the_corrected_values()
    {
        var proposal = AddPendingExpense();

        var result = await Handle(CommandFrom(proposal) with
        {
            Amount = 18250m,
            Currency = Currency.USD,
            ExpenseCategory = ExpenseCategory.Alimentos,
            Description = "Compra mensual en Coto",
            OccurredOn = new DateOnly(2026, 10, 2),
            PaymentMethod = PaymentMethod.CreditCard,
        });

        result.IsSuccess.ShouldBeTrue();
        proposal.Status.ShouldBe(TransactionStatus.Confirmed);
        proposal.Amount.ShouldBe(Money.From(18250m, Currency.USD));
        proposal.ExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        proposal.Description.ShouldBe("Compra mensual en Coto");
        proposal.OccurredOn.ShouldBe(new DateOnly(2026, 10, 2));
        proposal.PaymentMethod.ShouldBe(PaymentMethod.CreditCard);
        result.Value.Category.ShouldBe(nameof(ExpenseCategory.Alimentos));
        result.Value.Amount.ShouldBe(18250m);
    }

    [Fact]
    public async Task The_type_can_be_corrected_from_expense_to_income()
    {
        var proposal = AddPendingExpense();

        var result = await Handle(CommandFrom(proposal) with
        {
            Type = TransactionType.Income,
            ExpenseCategory = null,
            IncomeCategory = IncomeCategory.Freelance,
        });

        result.IsSuccess.ShouldBeTrue();
        proposal.Type.ShouldBe(TransactionType.Income);
        proposal.IncomeCategory.ShouldBe(IncomeCategory.Freelance);
        proposal.ExpenseCategory.ShouldBeNull();
    }

    [Fact]
    public async Task Correcting_the_category_keeps_the_confidence_the_AI_reported()
    {
        var proposal = AddPendingExpense();
        proposal.SuggestExpenseCategory(ExpenseCategory.Otros, 0.3, "claude-sonnet-5", Now.AddHours(-1));

        var result = await Handle(CommandFrom(proposal) with { ExpenseCategory = ExpenseCategory.Salud });

        result.Value.AiConfidence.ShouldBe(0.3);
        result.Value.AiConfidenceLevel.ShouldBe(ConfidenceLevel.Low);
        proposal.AiModel.ShouldBe("claude-sonnet-5");
    }

    [Fact]
    public async Task The_source_and_external_reference_survive_the_confirmation()
    {
        var proposal = PendingTransactionFactory.PendingExpense(
            _user.UserId!.Value, TransactionSource.Gmail, externalReference: "gmail-message-123");
        _transactions.Transactions.Add(proposal);

        await Handle(CommandFrom(proposal) with { Description = "Factura de luz" });

        proposal.Source.ShouldBe(TransactionSource.Gmail);
        proposal.ExternalReference.ShouldBe("gmail-message-123");
    }

    [Fact]
    public async Task Returns_conflict_when_the_transaction_was_already_confirmed()
    {
        var proposal = AddPendingExpense();
        proposal.Confirm(Now.AddHours(-1));

        var result = await Handle(CommandFrom(proposal) with { Amount = 1m });

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        proposal.Amount.Amount.ShouldBe(15400.50m);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_not_found_for_a_discarded_proposal()
    {
        var proposal = AddPendingExpense();
        proposal.Discard(Now.AddHours(-1));

        var result = await Handle(CommandFrom(proposal));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        proposal.Status.ShouldBe(TransactionStatus.Discarded);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_forbidden_when_the_proposal_belongs_to_another_employee()
    {
        var proposal = PendingTransactionFactory.PendingExpense(Guid.CreateVersion7());
        _transactions.Transactions.Add(proposal);

        var result = await Handle(CommandFrom(proposal));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_not_found_when_the_transaction_does_not_exist()
    {
        var command = CommandFrom(PendingTransactionFactory.PendingExpense(_user.UserId!.Value));

        var result = await Handle(command);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Returns_forbidden_when_the_user_is_not_authenticated()
    {
        var proposal = AddPendingExpense();
        var handler = new ConfirmTransactionHandler(
            _transactions, _unitOfWork, new FakeCurrentUser(), new FakeDateTimeProvider(Now));

        var result = await handler.Handle(CommandFrom(proposal), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
    }

    private Transaction AddPendingExpense()
    {
        var proposal = PendingTransactionFactory.PendingExpense(_user.UserId!.Value);
        _transactions.Transactions.Add(proposal);

        return proposal;
    }

    private Task<Result<TransactionResponse>> Handle(ConfirmTransactionCommand command) =>
        new ConfirmTransactionHandler(_transactions, _unitOfWork, _user, new FakeDateTimeProvider(Now))
            .Handle(command, CancellationToken.None);

    private static ConfirmTransactionCommand CommandFrom(Transaction transaction) => new(
        transaction.Id,
        transaction.Type,
        transaction.Amount.Amount,
        transaction.Amount.Currency,
        transaction.ExpenseCategory,
        transaction.IncomeCategory,
        transaction.Description,
        transaction.OccurredOn,
        transaction.PaymentMethod);
}
