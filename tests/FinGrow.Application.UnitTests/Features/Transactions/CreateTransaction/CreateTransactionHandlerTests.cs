namespace FinGrow.Application.UnitTests.Features.Transactions.CreateTransaction;

using FinGrow.Application.DTOs.Transactions;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Services.Transactions;
using FinGrow.Domain.ValueObjects;
using Moq;

public class CreateTransactionHandlerTests
{
    private readonly Mock<ITransactionService> _transactionService = new();
    private readonly CreateTransactionHandler _handler;

    public CreateTransactionHandlerTests() => _handler = new CreateTransactionHandler(_transactionService.Object);

    [Fact]
    public async Task A_manual_expense_is_persisted_as_confirmed_with_manual_source()
    {
        var employeeId = Guid.CreateVersion7();
        var request = new CreateTransactionRequest(
            employeeId,
            new CreateTransactionRequestDto(
                TransactionType.Expense,
                Amount: 1500m,
                Currency.ARS,
                ExpenseCategory.Alimentos,
                IncomeCategory: null,
                Description: "Supermercado",
                OccurredOn: new DateOnly(2026, 9, 1),
                PaymentMethod.Cash));

        var expectedTransaction = Transaction.RegisterExpense(
            employeeId,
            Money.From(1500m, Currency.ARS),
            ExpenseCategory.Alimentos,
            "Supermercado",
            new DateOnly(2026, 9, 1),
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            DateTimeOffset.UtcNow);

        _transactionService
            .Setup(service => service.CreateAsync(It.IsAny<CreateTransactionInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransaction);

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(TransactionSource.Manual);
        result.Value.Status.ShouldBe(TransactionStatus.Confirmed);
        result.Value.Category.ShouldBe(nameof(ExpenseCategory.Alimentos));

        _transactionService.Verify(
            service => service.CreateAsync(
                It.Is<CreateTransactionInput>(input =>
                    input.EmployeeId == employeeId
                    && input.Type == TransactionType.Expense
                    && input.ExpenseCategory == ExpenseCategory.Alimentos
                    && input.Amount == 1500m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task A_manual_income_is_persisted_as_confirmed_with_manual_source()
    {
        var request = new CreateTransactionRequest(
            Guid.CreateVersion7(),
            new CreateTransactionRequestDto(
                TransactionType.Income,
                Amount: 200000m,
                Currency.ARS,
                ExpenseCategory: null,
                IncomeCategory.Salario,
                Description: "Sueldo",
                OccurredOn: new DateOnly(2026, 9, 1),
                PaymentMethod.BankTransfer));

        var expectedTransaction = Transaction.RegisterIncome(
            request.EmployeeId,
            Money.From(200000m, Currency.ARS),
            IncomeCategory.Salario,
            "Sueldo",
            new DateOnly(2026, 9, 1),
            PaymentMethod.BankTransfer,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            DateTimeOffset.UtcNow);

        _transactionService
            .Setup(service => service.CreateAsync(It.IsAny<CreateTransactionInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransaction);

        var result = await _handler.Handle(request, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Source.ShouldBe(TransactionSource.Manual);
        result.Value.Status.ShouldBe(TransactionStatus.Confirmed);
        result.Value.Category.ShouldBe(nameof(IncomeCategory.Salario));
    }
}
