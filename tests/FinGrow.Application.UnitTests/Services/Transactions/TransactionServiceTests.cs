namespace FinGrow.Application.UnitTests.Services.Transactions;

using FinGrow.Application.Services.Transactions;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories.Transactions;
using FinGrow.Domain.Services.Transactions;
using FinGrow.Domain.ValueObjects;
using Moq;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repository = new();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new TransactionService(_repository.Object);
    }

    [Fact]
    public async Task Creating_a_manual_expense_persists_it_as_confirmed_with_manual_source()
    {
        var employeeId = Guid.CreateVersion7();

        var transaction = await _service.CreateAsync(new CreateTransactionInput(
            employeeId,
            TransactionType.Expense,
            Amount: 1500m,
            Currency.ARS,
            ExpenseCategory.Alimentos,
            IncomeCategory: null,
            Description: "Supermercado",
            OccurredOn: new DateOnly(2026, 9, 1),
            PaymentMethod.Cash));

        transaction.EmployeeId.ShouldBe(employeeId);
        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        transaction.Source.ShouldBe(TransactionSource.Manual);
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);

        _repository.Verify(
            repository => repository.AddAsync(
                It.Is<Transaction>(stored => stored.Id == transaction.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Creating_a_manual_income_persists_it_as_confirmed_with_manual_source()
    {
        var transaction = await _service.CreateAsync(new CreateTransactionInput(
            Guid.CreateVersion7(),
            TransactionType.Income,
            Amount: 200000m,
            Currency.ARS,
            ExpenseCategory: null,
            IncomeCategory.Salario,
            Description: "Sueldo",
            OccurredOn: new DateOnly(2026, 9, 1),
            PaymentMethod.BankTransfer));

        transaction.Type.ShouldBe(TransactionType.Income);
        transaction.IncomeCategory.ShouldBe(IncomeCategory.Salario);
        transaction.Source.ShouldBe(TransactionSource.Manual);
        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task GetByIdAsync_delegates_to_the_repository()
    {
        var expected = Transaction.RegisterExpense(
            Guid.CreateVersion7(),
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Otros,
            "Gasto",
            new DateOnly(2026, 9, 1),
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            DateTimeOffset.UtcNow);

        _repository
            .Setup(repository => repository.GetByIdAsync(expected.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var found = await _service.GetByIdAsync(expected.Id);

        found.ShouldBe(expected);
    }

    [Fact]
    public async Task ListByEmployeeAsync_delegates_to_the_repository()
    {
        var employeeId = Guid.CreateVersion7();
        IReadOnlyList<Transaction> expected = new List<Transaction>
        {
            Transaction.RegisterExpense(
                employeeId,
                Money.From(100m, Currency.ARS),
                ExpenseCategory.Otros,
                "Gasto",
                new DateOnly(2026, 9, 1),
                PaymentMethod.Cash,
                TransactionSource.Manual,
                TransactionStatus.Confirmed,
                DateTimeOffset.UtcNow),
        };

        _repository
            .Setup(repository => repository.ListByEmployeeAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.ListByEmployeeAsync(employeeId);

        result.ShouldBe(expected);
    }
}
