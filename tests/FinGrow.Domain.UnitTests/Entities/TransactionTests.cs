namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class TransactionTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 3, 15);

    [Fact]
    public void An_expense_keeps_an_expense_category_and_no_income_category()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.Type.ShouldBe(TransactionType.Expense);
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Servicios);
        transaction.IncomeCategory.ShouldBeNull();
    }

    [Fact]
    public void An_income_keeps_an_income_category_and_no_expense_category()
    {
        var transaction = Transaction.RegisterIncome(
            EmployeeId,
            Money.From(850000m, Currency.ARS),
            IncomeCategory.Salario,
            "Sueldo de marzo",
            Today,
            PaymentMethod.BankTransfer,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);

        transaction.Type.ShouldBe(TransactionType.Income);
        transaction.IncomeCategory.ShouldBe(IncomeCategory.Salario);
        transaction.ExpenseCategory.ShouldBeNull();
    }

    [Fact]
    public void A_transaction_with_a_zero_amount_is_not_registered()
    {
        Should.Throw<DomainException>(() => RegisterExpense(Money.Zero(Currency.ARS)));
    }

    [Fact]
    public void A_transaction_without_a_description_is_not_registered()
    {
        Should.Throw<DomainException>(() => Transaction.RegisterExpense(
            EmployeeId,
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Otros,
            "   ",
            Today,
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now));
    }

    [Fact]
    public void A_transaction_always_belongs_to_an_employee()
    {
        Should.Throw<DomainException>(() => Transaction.RegisterExpense(
            Guid.Empty,
            Money.From(100m, Currency.ARS),
            ExpenseCategory.Otros,
            "Cafe",
            Today,
            PaymentMethod.Cash,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now));
    }

    [Fact]
    public void A_transaction_entered_manually_by_the_employee_is_created_confirmed()
    {
        var transaction = Transaction.RegisterIncome(
            EmployeeId,
            Money.From(850000m, Currency.ARS),
            IncomeCategory.Salario,
            "Sueldo de marzo",
            Today,
            PaymentMethod.BankTransfer,
            TransactionSource.Manual,
            TransactionStatus.Confirmed,
            Now);

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.IsPending.ShouldBeFalse();
    }

    [Fact]
    public void A_transaction_proposed_by_an_integration_is_created_pending()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.IsPending.ShouldBeTrue();
    }

    [Fact]
    public void Confirming_a_proposal_makes_it_count()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        var confirmedAt = Now.AddMinutes(10);

        transaction.Confirm(confirmedAt);

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.UpdatedAt.ShouldBe(confirmedAt);
    }

    [Fact]
    public void An_already_confirmed_transaction_cannot_be_confirmed_again()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Confirm(Now);

        Should.Throw<DomainException>(() => transaction.Confirm(Now.AddMinutes(1)));
    }

    [Fact]
    public void An_expense_can_be_recategorized_when_the_employee_corrects_the_AI()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.RecategorizeExpense(ExpenseCategory.Vivienda, Now.AddMinutes(5));

        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Vivienda);
        transaction.UpdatedAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void An_expense_does_not_accept_an_income_category()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        Should.Throw<DomainException>(() => transaction.RecategorizeIncome(IncomeCategory.Salario, Now));
    }

    private static Transaction RegisterExpense(Money amount) => Transaction.RegisterExpense(
        EmployeeId,
        amount,
        ExpenseCategory.Servicios,
        "Factura de luz",
        Today,
        PaymentMethod.DebitCard,
        TransactionSource.Gmail,
        TransactionStatus.Pending,
        Now,
        externalReference: "gmail-message-123");
}
