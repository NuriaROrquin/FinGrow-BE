namespace FinGrow.Application.UnitTests.Validations.Transactions;

using FinGrow.Application.Features.Transactions.ConfirmTransaction;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class ConfirmTransactionValidatorTests
{
    private readonly ConfirmTransactionValidator _validator = new();

    private static ConfirmTransactionCommand ValidExpenseCommand() => new(
        Guid.CreateVersion7(),
        TransactionType.Expense,
        Amount: 15400.50m,
        Currency.ARS,
        ExpenseCategory.Alimentos,
        IncomeCategory: null,
        Description: "Supermercado Coto",
        OccurredOn: new DateOnly(2026, 10, 4),
        PaymentMethod.DebitCard);

    [Fact]
    public void A_valid_expense_passes()
    {
        _validator.Validate(ValidExpenseCommand()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_valid_income_passes()
    {
        var command = ValidExpenseCommand() with
        {
            Type = TransactionType.Income,
            ExpenseCategory = null,
            IncomeCategory = IncomeCategory.Salario,
        };

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void An_amount_that_is_not_positive_is_rejected(int amount)
    {
        var result = _validator.Validate(ValidExpenseCommand() with { Amount = amount });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(ConfirmTransactionCommand.Amount));
    }

    [Fact]
    public void An_empty_description_is_rejected()
    {
        var result = _validator.Validate(ValidExpenseCommand() with { Description = " " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(ConfirmTransactionCommand.Description));
    }

    [Fact]
    public void A_description_longer_than_the_limit_is_rejected()
    {
        var description = new string('a', Transaction.MaxDescriptionLength + 1);

        _validator.Validate(ValidExpenseCommand() with { Description = description }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void A_missing_date_is_rejected()
    {
        var result = _validator.Validate(ValidExpenseCommand() with { OccurredOn = default });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(ConfirmTransactionCommand.OccurredOn));
    }

    [Fact]
    public void An_expense_without_category_is_rejected()
    {
        var result = _validator.Validate(ValidExpenseCommand() with { ExpenseCategory = null });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(ConfirmTransactionCommand.ExpenseCategory));
    }

    [Fact]
    public void An_expense_with_an_income_category_is_rejected()
    {
        var result = _validator.Validate(ValidExpenseCommand() with { IncomeCategory = IncomeCategory.Salario });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(ConfirmTransactionCommand.IncomeCategory));
    }

    [Fact]
    public void An_income_without_category_is_rejected()
    {
        var command = ValidExpenseCommand() with { Type = TransactionType.Income, ExpenseCategory = null };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_category_is_rejected()
    {
        var result = _validator.Validate(ValidExpenseCommand() with { ExpenseCategory = (ExpenseCategory)999 });

        result.IsValid.ShouldBeFalse();
    }
}
