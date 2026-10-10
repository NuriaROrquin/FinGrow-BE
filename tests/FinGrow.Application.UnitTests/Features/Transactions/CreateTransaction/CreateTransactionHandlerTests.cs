namespace FinGrow.Application.UnitTests.Features.Transactions.CreateTransaction;

using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Common;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class CreateTransactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeTransactionReceiptRepository _receipts = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateTransactionHandler _handler;

    public CreateTransactionHandlerTests() =>
        _handler = new CreateTransactionHandler(_transactions, _receipts, _unitOfWork, new FakeDateTimeProvider(Now));

    [Fact]
    public async Task A_manual_expense_is_persisted_as_confirmed_with_manual_source()
    {
        var employeeId = Guid.CreateVersion7();
        var command = new CreateTransactionCommand(
            employeeId,
            TransactionType.Expense,
            Amount: 1500m,
            Currency.ARS,
            ExpenseCategory.Alimentos,
            IncomeCategory: null,
            Description: "Supermercado",
            OccurredOn: new DateOnly(2026, 9, 1),
            PaymentMethod.Cash);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(TransactionSource.Manual);
        result.Value.Status.ShouldBe(TransactionStatus.Confirmed);
        result.Value.Category.ShouldBe(nameof(ExpenseCategory.Alimentos));
        _unitOfWork.SaveCount.ShouldBe(1);

        var stored = _transactions.Transactions.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task A_manual_income_is_persisted_as_confirmed_with_manual_source()
    {
        var command = new CreateTransactionCommand(
            Guid.CreateVersion7(),
            TransactionType.Income,
            Amount: 200000m,
            Currency.ARS,
            ExpenseCategory: null,
            IncomeCategory.Salario,
            Description: "Sueldo",
            OccurredOn: new DateOnly(2026, 9, 1),
            PaymentMethod.BankTransfer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(TransactionSource.Manual);
        result.Value.Status.ShouldBe(TransactionStatus.Confirmed);
        result.Value.Category.ShouldBe(nameof(IncomeCategory.Salario));
    }

    [Fact]
    public async Task A_transaction_created_with_a_receipt_is_attached_to_it_in_the_same_save()
    {
        var employeeId = Guid.CreateVersion7();
        var receipt = TransactionReceipt.Upload(employeeId, "image/jpeg", 2048, "ticket.jpg", Now);
        _receipts.Receipts.Add(receipt);

        var result = await _handler.Handle(ExpenseWithReceipt(employeeId, receipt.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        receipt.TransactionId.ShouldBe(result.Value.Id);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_receipt_of_another_employee_is_rejected_and_nothing_is_created()
    {
        var receipt = TransactionReceipt.Upload(Guid.CreateVersion7(), "image/jpeg", 2048, null, Now);
        _receipts.Receipts.Add(receipt);

        var result = await _handler.Handle(ExpenseWithReceipt(Guid.CreateVersion7(), receipt.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _transactions.Transactions.ShouldBeEmpty();
        receipt.TransactionId.ShouldBeNull();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task An_unknown_receipt_is_rejected_and_nothing_is_created()
    {
        var result = await _handler.Handle(ExpenseWithReceipt(Guid.CreateVersion7(), Guid.CreateVersion7()), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _transactions.Transactions.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private static CreateTransactionCommand ExpenseWithReceipt(Guid employeeId, Guid receiptId) => new(
        employeeId,
        TransactionType.Expense,
        Amount: 4500m,
        Currency.ARS,
        ExpenseCategory.Alimentos,
        IncomeCategory: null,
        Description: "Verduleria",
        OccurredOn: new DateOnly(2026, 9, 1),
        PaymentMethod.DebitCard,
        receiptId);
}
