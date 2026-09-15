namespace FinGrow.Application.UnitTests.Features.Transactions.GetTransactionById;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Services.Transactions;
using FinGrow.Domain.ValueObjects;
using Moq;

public class GetTransactionByIdHandlerTests
{
    [Fact]
    public async Task Returns_the_transaction_with_manual_source_after_a_manual_load()
    {
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

        var transactionService = new Mock<ITransactionService>();
        transactionService
            .Setup(service => service.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var getByIdHandler = new GetTransactionByIdHandler(transactionService.Object);
        var result = await getByIdHandler.Handle(new GetTransactionByIdRequest(transaction.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(TransactionSource.Manual);
    }

    [Fact]
    public async Task Returns_not_found_for_a_missing_transaction()
    {
        var missingId = Guid.CreateVersion7();
        var transactionService = new Mock<ITransactionService>();
        transactionService
            .Setup(service => service.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var getByIdHandler = new GetTransactionByIdHandler(transactionService.Object);
        var result = await getByIdHandler.Handle(new GetTransactionByIdRequest(missingId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
