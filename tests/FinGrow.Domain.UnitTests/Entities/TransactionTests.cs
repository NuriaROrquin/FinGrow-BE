namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.Events;
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
    public void A_pending_transaction_can_be_confirmed_while_editing_it()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.Correct(
            TransactionType.Expense,
            Money.From(5000m, Currency.ARS),
            ExpenseCategory.Servicios,
            null,
            "Factura de luz actualizada",
            Today,
            PaymentMethod.DebitCard,
            TransactionStatus.Confirmed,
            Now.AddMinutes(5));

        transaction.Status.ShouldBe(TransactionStatus.Confirmed);
        transaction.UpdatedAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void A_confirmed_transaction_cannot_be_set_back_to_pending()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Confirm(Now);

        Should.Throw<DomainException>(() => transaction.Correct(
            TransactionType.Expense,
            Money.From(4500m, Currency.ARS),
            ExpenseCategory.Servicios,
            null,
            "Factura de luz",
            Today,
            PaymentMethod.DebitCard,
            TransactionStatus.Pending,
            Now.AddMinutes(1)));
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

    [Fact]
    public void Discarding_a_proposal_keeps_the_row_so_it_is_not_proposed_again()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        var discardedAt = Now.AddMinutes(3);

        transaction.Discard(discardedAt);

        transaction.Status.ShouldBe(TransactionStatus.Discarded);
        transaction.IsPending.ShouldBeFalse();
        transaction.ExternalReference.ShouldBe("gmail-message-123");
        transaction.UpdatedAt.ShouldBe(discardedAt);
    }

    [Fact]
    public void Only_a_pending_transaction_can_be_discarded()
    {
        var confirmed = RegisterExpense(Money.From(4500m, Currency.ARS));
        confirmed.Confirm(Now);
        var discarded = RegisterExpense(Money.From(4500m, Currency.ARS));
        discarded.Discard(Now);
        var eliminated = RegisterExpense(Money.From(4500m, Currency.ARS));
        eliminated.Eliminate(Now);

        Should.Throw<DomainException>(() => confirmed.Discard(Now.AddMinutes(1)));
        Should.Throw<DomainException>(() => discarded.Discard(Now.AddMinutes(1)));
        Should.Throw<DomainException>(() => eliminated.Discard(Now.AddMinutes(1)));
    }

    [Fact]
    public void A_discarded_transaction_cannot_be_confirmed()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Discard(Now);

        Should.Throw<DomainException>(() => transaction.Confirm(Now.AddMinutes(1)));
        transaction.Status.ShouldBe(TransactionStatus.Discarded);
    }

    [Fact]
    public void An_eliminated_transaction_cannot_be_confirmed()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Eliminate(Now);

        Should.Throw<DomainException>(() => transaction.Confirm(Now.AddMinutes(1)));
    }

    [Fact]
    public void A_discarded_transaction_cannot_be_edited()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Discard(Now);

        Should.Throw<DomainException>(() => transaction.Correct(
            TransactionType.Expense,
            Money.From(4500m, Currency.ARS),
            ExpenseCategory.Servicios,
            null,
            "Factura de luz",
            Today,
            PaymentMethod.DebitCard,
            TransactionStatus.Confirmed,
            Now.AddMinutes(1)));
    }

    [Fact]
    public void Editing_a_transaction_cannot_discard_it()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        Should.Throw<DomainException>(() => transaction.Correct(
            TransactionType.Expense,
            Money.From(4500m, Currency.ARS),
            ExpenseCategory.Servicios,
            null,
            "Factura de luz",
            Today,
            PaymentMethod.DebitCard,
            TransactionStatus.Discarded,
            Now.AddMinutes(1)));
        transaction.Status.ShouldBe(TransactionStatus.Pending);
    }

    [Fact]
    public void A_transaction_that_did_not_go_through_the_AI_has_no_confidence()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.AiConfidence.ShouldBeNull();
        transaction.AiConfidenceLevel.ShouldBeNull();
        transaction.AiModel.ShouldBeNull();
    }

    [Fact]
    public void An_AI_suggestion_sets_the_category_and_records_confidence_and_model()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.SuggestExpenseCategory(ExpenseCategory.Alimentos, 0.92, " claude-sonnet-5 ", Now.AddMinutes(2));

        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Alimentos);
        transaction.AiConfidence.ShouldBe(0.92);
        transaction.AiConfidenceLevel.ShouldBe(ConfidenceLevel.High);
        transaction.AiModel.ShouldBe("claude-sonnet-5");
        transaction.Status.ShouldBe(TransactionStatus.Pending);
        transaction.UpdatedAt.ShouldBe(Now.AddMinutes(2));
    }

    [Theory]
    [InlineData(0.0, ConfidenceLevel.Low)]
    [InlineData(0.49, ConfidenceLevel.Low)]
    [InlineData(0.5, ConfidenceLevel.Medium)]
    [InlineData(0.79, ConfidenceLevel.Medium)]
    [InlineData(0.8, ConfidenceLevel.High)]
    [InlineData(1.0, ConfidenceLevel.High)]
    public void The_confidence_level_follows_the_agreed_thresholds(double confidence, ConfidenceLevel expected)
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        transaction.SuggestExpenseCategory(ExpenseCategory.Alimentos, confidence, "model", Now);

        transaction.AiConfidenceLevel.ShouldBe(expected);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void A_confidence_outside_zero_to_one_is_rejected(double confidence)
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        Should.Throw<DomainException>(() =>
            transaction.SuggestExpenseCategory(ExpenseCategory.Alimentos, confidence, "model", Now));
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Servicios);
        transaction.AiConfidence.ShouldBeNull();
    }

    [Fact]
    public void The_AI_cannot_suggest_on_a_transaction_the_employee_already_reviewed()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Confirm(Now);

        Should.Throw<DomainException>(() =>
            transaction.SuggestExpenseCategory(ExpenseCategory.Alimentos, 0.9, "model", Now.AddMinutes(1)));
        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Servicios);
    }

    [Fact]
    public void The_AI_cannot_suggest_an_expense_category_for_an_income()
    {
        var income = Transaction.RegisterIncome(
            EmployeeId,
            Money.From(6000m, Currency.ARS),
            IncomeCategory.Otros,
            "Cobro",
            Today,
            PaymentMethod.DigitalWallet,
            TransactionSource.MercadoPago,
            TransactionStatus.Pending,
            Now);

        Should.Throw<DomainException>(() =>
            income.SuggestExpenseCategory(ExpenseCategory.Alimentos, 0.9, "model", Now));
    }

    [Fact]
    public void Correcting_the_category_keeps_what_the_AI_originally_reported()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.SuggestExpenseCategory(ExpenseCategory.Otros, 0.3, "model", Now);

        transaction.Correct(
            TransactionType.Expense,
            Money.From(4500m, Currency.ARS),
            ExpenseCategory.Vivienda,
            null,
            "Factura de luz",
            Today,
            PaymentMethod.DebitCard,
            TransactionStatus.Confirmed,
            Now.AddMinutes(5));

        transaction.ExpenseCategory.ShouldBe(ExpenseCategory.Vivienda);
        transaction.AiConfidence.ShouldBe(0.3);
        transaction.AiModel.ShouldBe("model");
    }

    [Fact]
    public void Eliminating_a_transaction_keeps_it_for_auditing_with_an_eliminated_status()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        var deletedAt = Now.AddMinutes(8);

        transaction.Eliminate(deletedAt);

        transaction.Status.ShouldBe(TransactionStatus.Eliminated);
        transaction.UpdatedAt.ShouldBe(deletedAt);
    }

    [Fact]
    public void An_eliminated_transaction_cannot_be_eliminated_again()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));
        transaction.Eliminate(Now);

        Should.Throw<DomainException>(() => transaction.Eliminate(Now.AddMinutes(1)));
    }

    [Fact]
    public void A_proposed_transaction_announces_that_it_is_waiting_for_review()
    {
        var transaction = RegisterExpense(Money.From(4500m, Currency.ARS));

        var proposed = transaction.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<TransactionProposed>();
        proposed.TransactionId.ShouldBe(transaction.Id);
        proposed.EmployeeId.ShouldBe(EmployeeId);
        proposed.Source.ShouldBe(TransactionSource.Gmail);
        proposed.OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void A_transaction_registered_as_confirmed_has_nothing_to_announce()
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

        transaction.DequeueDomainEvents().ShouldBeEmpty();
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
