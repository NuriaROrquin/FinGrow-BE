namespace FinGrow.Application.UnitTests.Features.Transactions.RemoveTransactionReceipt;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.RemoveTransactionReceipt;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class RemoveTransactionReceiptHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Removing_leaves_the_receipt_loose_and_the_transaction_without_one()
    {
        var transaction = AddExpense(_user.UserId!.Value);
        var receipt = TransactionReceipt.Upload(_user.UserId!.Value, "image/png", 10, null, Now);
        receipt.AttachTo(transaction);
        _receipts.Receipts.Add(receipt);

        var result = await Handle(transaction.Id);

        result.IsSuccess.ShouldBeTrue();
        receipt.TransactionId.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Removing_from_a_transaction_without_a_receipt_does_nothing()
    {
        var transaction = AddExpense(_user.UserId!.Value);

        var result = await Handle(transaction.Id);

        result.IsSuccess.ShouldBeTrue();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_transaction_of_another_employee_is_reported_as_not_found()
    {
        var transaction = AddExpense(Guid.CreateVersion7());
        var receipt = TransactionReceipt.Upload(transaction.EmployeeId, "image/png", 10, null, Now);
        receipt.AttachTo(transaction);
        _receipts.Receipts.Add(receipt);

        var result = await Handle(transaction.Id);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        receipt.TransactionId.ShouldBe(transaction.Id);
    }

    private Transaction AddExpense(Guid employeeId)
    {
        var transaction = Transaction.RegisterExpense(
            employeeId,
            Money.From(4500m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Supermercado",
            new DateOnly(2026, 10, 9),
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);
        _transactions.Transactions.Add(transaction);

        return transaction;
    }

    private Task<Result> Handle(Guid transactionId) =>
        new RemoveTransactionReceiptHandler(_transactions, _receipts, _unitOfWork, _user)
            .Handle(new RemoveTransactionReceiptCommand(transactionId), CancellationToken.None);
}
