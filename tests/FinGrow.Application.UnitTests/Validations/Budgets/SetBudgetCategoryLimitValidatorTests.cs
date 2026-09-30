namespace FinGrow.Application.UnitTests.Validations.Budgets;

using FinGrow.Application.Features.Budgets.SetCategoryLimit;
using FinGrow.Application.Validations.Budgets;
using Domain.Enums;

public class SetBudgetCategoryLimitValidatorTests
{
    private readonly SetBudgetCategoryLimitValidator _validator = new();

    private static SetBudgetCategoryLimitCommand ValidCommand() =>
        new(Guid.CreateVersion7(), 2026, 9, ExpenseCategory.Alimentos, 150000m);

    [Fact]
    public void A_valid_command_passes()
    {
        _validator.Validate(ValidCommand()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-150000)]
    [InlineData(0)]
    public void A_limit_that_is_not_positive_is_rejected_with_a_clear_message(int amount)
    {
        var result = _validator.Validate(ValidCommand() with { Amount = amount });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error =>
            error.PropertyName == nameof(SetBudgetCategoryLimitCommand.Amount)
            && error.ErrorMessage == "El tope de una categoria tiene que ser un numero mayor a cero.");
    }

    [Fact]
    public void A_limit_that_does_not_fit_in_the_column_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Amount = decimal.MaxValue });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SetBudgetCategoryLimitCommand.Amount));
    }

    [Fact]
    public void An_unknown_category_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Category = (ExpenseCategory)999 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SetBudgetCategoryLimitCommand.Category));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void A_month_outside_one_to_twelve_is_rejected(int month)
    {
        var result = _validator.Validate(ValidCommand() with { Month = month });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SetBudgetCategoryLimitCommand.Month));
    }
}
