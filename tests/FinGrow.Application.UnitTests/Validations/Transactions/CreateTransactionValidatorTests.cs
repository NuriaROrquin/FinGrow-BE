namespace FinGrow.Application.UnitTests.Validations.Transactions;

using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Validations.Transactions;
using FinGrow.Domain.Enums;

public class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    private static CreateTransactionCommand ValidExpenseCommand() => new(
        Guid.CreateVersion7(),
        TransactionType.Expense,
        Amount: 100m,
        Currency.ARS,
        ExpenseCategory.Alimentos,
        IncomeCategory: null,
        Description: "Supermercado",
        OccurredOn: new DateOnly(2026, 9, 1),
        PaymentMethod.Cash);

    [Fact]
    public void A_valid_expense_command_passes()
    {
        var result = _validator.Validate(ValidExpenseCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_zero_amount_is_rejected()
    {
        var command = ValidExpenseCommand() with { Amount = 0m };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateTransactionCommand.Amount));
    }

    [Fact]
    public void A_negative_amount_is_rejected()
    {
        var command = ValidExpenseCommand() with { Amount = -50m };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_empty_description_is_rejected()
    {
        var command = ValidExpenseCommand() with { Description = "   " };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateTransactionCommand.Description));
    }

    [Fact]
    public void An_expense_without_expense_category_is_rejected()
    {
        var command = ValidExpenseCommand() with { ExpenseCategory = null };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateTransactionCommand.ExpenseCategory));
    }

    [Fact]
    public void An_expense_with_an_income_category_is_rejected()
    {
        var command = ValidExpenseCommand() with { IncomeCategory = IncomeCategory.Salario };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateTransactionCommand.IncomeCategory));
    }

    [Fact]
    public void An_income_without_income_category_is_rejected()
    {
        var command = ValidExpenseCommand() with
        {
            Type = TransactionType.Income,
            ExpenseCategory = null,
            IncomeCategory = null,
        };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateTransactionCommand.IncomeCategory));
    }

    [Fact]
    public void A_missing_occurred_on_is_rejected()
    {
        var command = ValidExpenseCommand() with { OccurredOn = default };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateTransactionCommand.OccurredOn));
    }
}
