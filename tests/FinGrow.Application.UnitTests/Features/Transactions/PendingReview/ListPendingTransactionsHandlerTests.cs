namespace FinGrow.Application.UnitTests.Features.Transactions.PendingReview;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.ListPendingTransactions;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;

public class ListPendingTransactionsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] NewestFirst = { "Nuevo", "Viejo" };

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Shows_type_amount_date_description_and_category_with_its_confidence()
    {
        var proposal = PendingTransactionFactory.PendingExpense(_user.UserId!.Value);
        proposal.SuggestExpenseCategory(ExpenseCategory.Alimentos, 0.95, "claude-sonnet-5", Now);
        _transactions.Transactions.Add(proposal);

        var result = await Handle();

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        var item = result.Value.Items.ShouldHaveSingleItem();
        item.Id.ShouldBe(proposal.Id);
        item.Type.ShouldBe(TransactionType.Expense);
        item.Amount.ShouldBe(15400.50m);
        item.Currency.ShouldBe(Currency.ARS);
        item.OccurredOn.ShouldBe(new DateOnly(2026, 10, 4));
        item.Description.ShouldBe("Supermercado Coto");
        item.Category.ShouldBe(nameof(ExpenseCategory.Alimentos));
        item.AiConfidence.ShouldBe(0.95);
        item.AiConfidenceLevel.ShouldBe(ConfidenceLevel.High);
        item.Source.ShouldBe(TransactionSource.MercadoPago);
        item.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public async Task A_proposal_the_AI_did_not_interpret_has_no_confidence()
    {
        _transactions.Transactions.Add(PendingTransactionFactory.PendingIncome(_user.UserId!.Value));

        var result = await Handle();

        var item = result.Value.Items.ShouldHaveSingleItem();
        item.Type.ShouldBe(TransactionType.Income);
        item.AiConfidence.ShouldBeNull();
        item.AiConfidenceLevel.ShouldBeNull();
    }

    [Theory]
    [InlineData(TransactionSource.ReceiptScan)]
    [InlineData(TransactionSource.Gmail)]
    [InlineData(TransactionSource.Telegram)]
    [InlineData(TransactionSource.WhatsApp)]
    [InlineData(TransactionSource.MercadoPago)]
    public async Task Proposals_from_every_automatic_channel_reach_the_inbox(TransactionSource source)
    {
        _transactions.Transactions.Add(PendingTransactionFactory.PendingExpense(_user.UserId!.Value, source));

        var result = await Handle();

        result.Value.Items.ShouldHaveSingleItem().Source.ShouldBe(source);
    }

    [Fact]
    public async Task Only_pending_transactions_are_listed()
    {
        var employeeId = _user.UserId!.Value;
        var pending = PendingTransactionFactory.PendingExpense(employeeId, description: "Pendiente");
        var confirmed = PendingTransactionFactory.PendingExpense(employeeId, description: "Confirmado");
        confirmed.Confirm(Now);
        var discarded = PendingTransactionFactory.PendingExpense(employeeId, description: "Descartado");
        discarded.Discard(Now);
        var eliminated = PendingTransactionFactory.PendingExpense(employeeId, description: "Eliminado");
        eliminated.Eliminate(Now);
        _transactions.Transactions.AddRange(new[] { pending, confirmed, discarded, eliminated });

        var result = await Handle();

        result.Value.Items.ShouldHaveSingleItem().Description.ShouldBe("Pendiente");
    }

    [Fact]
    public async Task Proposals_of_another_employee_are_not_listed()
    {
        _transactions.Transactions.Add(PendingTransactionFactory.PendingExpense(Guid.CreateVersion7()));

        var result = await Handle();

        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_most_recent_movements_come_first()
    {
        var employeeId = _user.UserId!.Value;
        _transactions.Transactions.Add(PendingTransactionFactory.PendingExpense(
            employeeId, description: "Viejo", occurredOn: new DateOnly(2026, 9, 1)));
        _transactions.Transactions.Add(PendingTransactionFactory.PendingExpense(
            employeeId, description: "Nuevo", occurredOn: new DateOnly(2026, 10, 1)));

        var result = await Handle();

        result.Value.Items.Select(item => item.Description).ShouldBe(NewestFirst);
    }

    [Fact]
    public async Task Returns_forbidden_when_the_user_is_not_authenticated()
    {
        var handler = new ListPendingTransactionsHandler(_transactions, new FakeCurrentUser());

        var result = await handler.Handle(new ListPendingTransactionsQuery(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private Task<Result<PendingTransactionsResponse>> Handle() =>
        new ListPendingTransactionsHandler(_transactions, _user)
            .Handle(new ListPendingTransactionsQuery(), CancellationToken.None);
}
