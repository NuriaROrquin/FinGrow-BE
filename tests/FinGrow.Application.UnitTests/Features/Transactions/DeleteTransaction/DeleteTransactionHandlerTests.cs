namespace FinGrow.Application.UnitTests.Features.Transactions.DeleteTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.DeleteTransaction;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class DeleteTransactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Deletes_the_employees_own_transaction_with_soft_delete()
    {
        var transactions = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var user = new FakeCurrentUser { UserId = Guid.CreateVersion7() };
        var clock = new FakeDateTimeProvider(Now);
        var transaction = NewExpense(user.UserId!.Value);
        transactions.Transactions.Add(transaction);

        var handler = new DeleteTransactionHandler(transactions, unitOfWork, user, clock);

        var result = await handler.Handle(new DeleteTransactionCommand(transaction.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        unitOfWork.SaveCount.ShouldBe(1);
        transaction.Status.ShouldBe(TransactionStatus.Eliminated);
        transaction.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Returns_not_found_when_the_transaction_belongs_to_another_employee()
    {
        var transactions = new FakeTransactionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var user = new FakeCurrentUser { UserId = Guid.CreateVersion7() };
        var transaction = NewExpense(Guid.CreateVersion7());
        transactions.Transactions.Add(transaction);

        var handler = new DeleteTransactionHandler(
            transactions,
            unitOfWork,
            user,
            new FakeDateTimeProvider(Now));

        var result = await handler.Handle(new DeleteTransactionCommand(transaction.Id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        unitOfWork.SaveCount.ShouldBe(0);
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task Returns_forbidden_when_the_user_is_not_authenticated()
    {
        var handler = new DeleteTransactionHandler(
            new FakeTransactionRepository(),
            new FakeUnitOfWork(),
            new FakeCurrentUser(),
            new FakeDateTimeProvider(Now));

        var result = await handler.Handle(new DeleteTransactionCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    private static Transaction NewExpense(Guid employeeId) => Transaction.RegisterExpense(
        employeeId,
        Money.From(9500m, Currency.ARS),
        ExpenseCategory.Otros,
        "Carga duplicada",
        new DateOnly(2026, 9, 20),
        PaymentMethod.DebitCard,
        TransactionSource.Manual,
        TransactionStatus.Confirmed,
        Now);
}
