namespace FinGrow.Application.UnitTests.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class GetTransactionByIdHandlerTests
{
    [Fact]
    public async Task Returns_the_transaction_with_manual_source_after_a_manual_load()
    {
        var transactions = new FakeTransactionRepository();
        var transaction = Transaction.RegisterExpense(
            Guid.CreateVersion7(),
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Supermercado",
            new DateOnly(2026, 9, 1),
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            DateTimeOffset.UtcNow);
        transactions.Transactions.Add(transaction);

        var handler = new GetTransactionByIdHandler(transactions);
        var result = await handler.Handle(new GetTransactionByIdCommand(transaction.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(TransactionSource.Manual);
    }

    [Fact]
    public async Task Returns_not_found_for_a_missing_transaction()
    {
        var handler = new GetTransactionByIdHandler(new FakeTransactionRepository());

        var result = await handler.Handle(new GetTransactionByIdCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
