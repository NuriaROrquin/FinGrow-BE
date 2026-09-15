namespace FinGrow.Application.UnitTests.Features.Transactions.ListTransactions;

using FinGrow.Application.Features.Transactions.ListTransactions;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Services.Transactions;
using FinGrow.Domain.ValueObjects;
using Moq;

public class ListTransactionsHandlerTests
{
    [Fact]
    public async Task Returns_the_transactions_from_the_service_wrapped_in_a_successful_result()
    {
        var employeeId = Guid.CreateVersion7();
        var transactions = new List<Transaction>
        {
            NewExpense(employeeId, new DateOnly(2026, 9, 5)),
            NewExpense(employeeId, new DateOnly(2026, 9, 1)),
        };

        var transactionService = new Mock<ITransactionService>();
        transactionService
            .Setup(service => service.ListByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var listHandler = new ListTransactionsHandler(transactionService.Object);
        var result = await listHandler.Handle(new ListTransactionsRequest(employeeId), CancellationToken.None);

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
