namespace FinGrow.Application.UnitTests.Features.Receipts;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts.AttachReceipt;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class AttachReceiptHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Attaches_a_loose_receipt_to_a_transaction_loaded_by_hand()
    {
        var receipt = AddReceipt(_user.UserId!.Value);
        var transaction = AddExpense(_user.UserId!.Value);

        var result = await Handle(receipt.Id, transaction.Id);

        result.IsSuccess.ShouldBeTrue();
        receipt.TransactionId.ShouldBe(transaction.Id);
        result.Value.TransactionId.ShouldBe(transaction.Id);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Returns_forbidden_for_a_receipt_of_another_employee()
    {
        var receipt = AddReceipt(Guid.CreateVersion7());
        var transaction = AddExpense(_user.UserId!.Value);

        var result = await Handle(receipt.Id, transaction.Id);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        receipt.TransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_forbidden_for_a_transaction_of_another_employee()
    {
        var receipt = AddReceipt(_user.UserId!.Value);
        var transaction = AddExpense(Guid.CreateVersion7());

        var result = await Handle(receipt.Id, transaction.Id);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        receipt.TransactionId.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_not_found_for_an_unknown_transaction()
    {
        var receipt = AddReceipt(_user.UserId!.Value);

        var result = await Handle(receipt.Id, Guid.CreateVersion7());

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Returns_not_found_for_an_unknown_receipt()
    {
        var transaction = AddExpense(_user.UserId!.Value);

        var result = await Handle(Guid.CreateVersion7(), transaction.Id);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Moves_a_receipt_attached_by_mistake_to_the_right_transaction()
    {
        var receipt = AddReceipt(_user.UserId!.Value);
        var wrong = AddExpense(_user.UserId!.Value);
        var right = AddExpense(_user.UserId!.Value);
        receipt.AttachTo(wrong);

        var result = await Handle(receipt.Id, right.Id);

        result.IsSuccess.ShouldBeTrue();
        receipt.TransactionId.ShouldBe(right.Id);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    private TransactionReceipt AddReceipt(Guid employeeId)
    {
        var receipt = TransactionReceipt.Upload(employeeId, "image/png", 10, null, Now);
        _receipts.Receipts.Add(receipt);

        return receipt;
    }

    private Transaction AddExpense(Guid employeeId)
    {
        var transaction = Transaction.RegisterExpense(
            employeeId,
            Money.From(4500m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Supermercado",
            new DateOnly(2026, 10, 7),
            PaymentMethod.DebitCard,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);
        _transactions.Transactions.Add(transaction);

        return transaction;
    }

    private Task<Result<TransactionReceiptResponse>> Handle(Guid receiptId, Guid transactionId) =>
        new AttachReceiptHandler(_receipts, _transactions, _unitOfWork, _user)
            .Handle(new AttachReceiptCommand(receiptId, transactionId), CancellationToken.None);
}
