namespace FinGrow.Application.UnitTests.Features.Transactions.CreateTransaction;

using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;

public class CreateTransactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateTransactionHandler _handler;

    public CreateTransactionHandlerTests() =>
        _handler = new CreateTransactionHandler(_transactions, _unitOfWork, new FakeDateTimeProvider(Now));

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
}
