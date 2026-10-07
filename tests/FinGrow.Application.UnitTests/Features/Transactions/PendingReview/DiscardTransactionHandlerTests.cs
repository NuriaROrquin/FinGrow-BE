namespace FinGrow.Application.UnitTests.Features.Transactions.PendingReview;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.DiscardTransaction;
using FinGrow.Application.Features.Transactions.ListPendingTransactions;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class DiscardTransactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] DiscardedReferences = { "mp-55" };

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Discarding_a_proposal_does_not_create_a_confirmed_transaction()
    {
        var proposal = AddPendingExpense();

        var result = await Handle(proposal.Id);

        result.IsSuccess.ShouldBeTrue();
        proposal.Status.ShouldBe(TransactionStatus.Discarded);
        proposal.UpdatedAt.ShouldBe(Now);
        _transactions.Transactions.ShouldNotContain(transaction => transaction.Status == TransactionStatus.Confirmed);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_discarded_proposal_leaves_the_inbox_and_the_transaction_list()
    {
        var proposal = AddPendingExpense();

        await Handle(proposal.Id);

        var inbox = await new ListPendingTransactionsHandler(_transactions, _user)
            .Handle(new ListPendingTransactionsQuery(), CancellationToken.None);
        inbox.Value.Items.ShouldBeEmpty();
        (await _transactions.ListByEmployeeAsync(_user.UserId!.Value)).ShouldBeEmpty();
        (await _transactions.GetByIdAsync(proposal.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task A_discarded_proposal_still_counts_as_known_so_it_is_not_proposed_again()
    {
        var proposal = PendingTransactionFactory.PendingExpense(_user.UserId!.Value, externalReference: "mp-55");
        _transactions.Transactions.Add(proposal);

        await Handle(proposal.Id);

        var known = await _transactions.ListExistingExternalReferencesAsync(
            _user.UserId!.Value, TransactionSource.MercadoPago, DiscardedReferences);
        known.ShouldContain("mp-55");
    }

    [Fact]
    public async Task Returns_conflict_when_the_transaction_was_already_confirmed()
    {
        var proposal = AddPendingExpense();
        proposal.Confirm(Now.AddHours(-1));

        var result = await Handle(proposal.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        proposal.Status.ShouldBe(TransactionStatus.Confirmed);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_not_found_when_the_proposal_was_already_discarded()
    {
        var proposal = AddPendingExpense();
        await Handle(proposal.Id);

        var result = await Handle(proposal.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Returns_forbidden_when_the_proposal_belongs_to_another_employee()
    {
        var proposal = PendingTransactionFactory.PendingExpense(Guid.CreateVersion7());
        _transactions.Transactions.Add(proposal);

        var result = await Handle(proposal.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        proposal.Status.ShouldBe(TransactionStatus.Pending);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_not_found_when_the_transaction_does_not_exist()
    {
        var result = await Handle(Guid.CreateVersion7());

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Returns_forbidden_when_the_user_is_not_authenticated()
    {
        var proposal = AddPendingExpense();
        var handler = new DiscardTransactionHandler(
            _transactions, _unitOfWork, new FakeCurrentUser(), new FakeDateTimeProvider(Now));

        var result = await handler.Handle(new DiscardTransactionCommand(proposal.Id), CancellationToken.None);

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

    private Task<Result> Handle(Guid id) =>
        new DiscardTransactionHandler(_transactions, _unitOfWork, _user, new FakeDateTimeProvider(Now))
            .Handle(new DiscardTransactionCommand(id), CancellationToken.None);
}
