namespace FinGrow.Application.UnitTests.Features.Transactions.UpdateTransaction;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Transactions.UpdateTransaction;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class UpdateTransactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.CreateVersion7() };

    [Fact]
    public async Task Editing_with_a_receipt_attaches_it_in_the_same_save()
    {
        var transaction = AddExpense();
        var receipt = AddReceipt(_user.UserId!.Value);

        var result = await Handle(transaction, receipt.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ReceiptId.ShouldBe(receipt.Id);
        receipt.TransactionId.ShouldBe(transaction.Id);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Editing_with_a_new_receipt_replaces_the_previous_one()
    {
        var transaction = AddExpense();
        var previous = AddReceipt(_user.UserId!.Value);
        previous.AttachTo(transaction);
        var replacement = AddReceipt(_user.UserId!.Value);

        var result = await Handle(transaction, replacement.Id);

        result.Value.ReceiptId.ShouldBe(replacement.Id);
        replacement.TransactionId.ShouldBe(transaction.Id);
        previous.TransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task Editing_without_a_receipt_keeps_the_one_it_had()
    {
        var transaction = AddExpense();
        var receipt = AddReceipt(_user.UserId!.Value);
        receipt.AttachTo(transaction);

        var result = await Handle(transaction, receiptId: null);

        result.Value.ReceiptId.ShouldBe(receipt.Id);
        receipt.TransactionId.ShouldBe(transaction.Id);
    }

    [Fact]
    public async Task A_receipt_of_another_employee_is_rejected_and_nothing_changes()
    {
        var transaction = AddExpense();
        var foreign = AddReceipt(Guid.CreateVersion7());

        var result = await Handle(transaction, foreign.Id, description: "No se tiene que guardar");

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        foreign.TransactionId.ShouldBeNull();
        transaction.Description.ShouldBe("Supermercado");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_unknown_receipt_is_rejected()
    {
        var transaction = AddExpense();

        var result = await Handle(transaction, Guid.CreateVersion7());

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private Transaction AddExpense()
    {
        var transaction = Transaction.RegisterExpense(
            _user.UserId!.Value,
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

    private TransactionReceipt AddReceipt(Guid employeeId)
    {
        var receipt = TransactionReceipt.Upload(employeeId, "image/png", 10, null, Now);
        _receipts.Receipts.Add(receipt);

        return receipt;
    }

    private Task<Result<TransactionResponse>> Handle(Transaction transaction, Guid? receiptId, string description = "Verduleria") =>
        new UpdateTransactionHandler(_transactions, _receipts, _unitOfWork, _user, new FakeDateTimeProvider(Now))
            .Handle(
                new UpdateTransactionCommand(
                    transaction.Id,
                    TransactionType.Expense,
                    4500m,
                    Currency.ARS,
                    ExpenseCategory.Alimentos,
                    IncomeCategory: null,
                    description,
                    new DateOnly(2026, 10, 9),
                    PaymentMethod.Cash,
                    TransactionStatus.Confirmed,
                    receiptId),
                CancellationToken.None);
}
