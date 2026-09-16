namespace FinGrow.Application.UnitTests.Features.Transactions.ListTransactions;

using FinGrow.Application.Features.Transactions.ListTransactions;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class ListTransactionsHandlerTests
{
    [Fact]
    public async Task Lists_only_the_requested_employees_transactions_newest_first()
    {
        var transactions = new FakeTransactionRepository();
        var employeeId = Guid.CreateVersion7();
        var otherEmployeeId = Guid.CreateVersion7();

        transactions.Transactions.Add(NewExpense(employeeId, new DateOnly(2026, 9, 1)));
        transactions.Transactions.Add(NewExpense(employeeId, new DateOnly(2026, 9, 5)));
        transactions.Transactions.Add(NewExpense(otherEmployeeId, new DateOnly(2026, 9, 10)));

        var handler = new ListTransactionsHandler(transactions);
        var result = await handler.Handle(new ListTransactionsCommand(employeeId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].OccurredOn.ShouldBe(new DateOnly(2026, 9, 5));
        result.Value[1].OccurredOn.ShouldBe(new DateOnly(2026, 9, 1));
    }

    private static Transaction NewExpense(Guid employeeId, DateOnly occurredOn) => Transaction.RegisterExpense(
        employeeId,
        Money.From(100m, Currency.ARS),
        ExpenseCategory.Otros,
        "Gasto",
        occurredOn,
        PaymentMethod.Cash,
        TransactionSource.Manual,
        TransactionStatus.Confirmed,
        DateTimeOffset.UtcNow);
}
